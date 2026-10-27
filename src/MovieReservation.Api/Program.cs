using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MovieReservation.Api.Middleware;
using MovieReservation.Application;
using MovieReservation.Infrastructure;
using MovieReservation.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key chưa được cấu hình.");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ClockSkew = TimeSpan.FromSeconds(30),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});
builder.Services.AddAuthorization(options => options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin")));

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "Enter JWT access token"
    });

    options.AddSecurityRequirement(document =>
        new Microsoft.OpenApi.OpenApiSecurityRequirement
        {
            [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

builder.Services.AddProblemDetails(); // dịch vụ ProblemDetails (RFC 9457), UseStatusCodePages dùng để điền body lỗi
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler(); // đứng đầu dây chuyền để bọc ngoài MapControllers: exception từ handler bay ngược ra sẽ đi qua đây
app.UseStatusCodePages();  // lỗi ASP.NET tự trả (401/403/404, không có exception, body rỗng) → điền ProblemDetails


// Migrate + seed admin: chạy 1 lần lúc khởi động. Tạo scope riêng vì lúc này chưa có request nào
// (AppDbContext và AdminSeeder đều là Scoped).
using (var scope = app.Services.CreateScope())
{
    // Migrate TRƯỚC seed: DB mới trên server chưa có bảng nào, seeder cần bảng Users.
    // Chỉ áp các migration chưa có trong __EFMigrationsHistory → chạy lại nhiều lần vẫn an toàn.
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<AdminSeeder>().SeedAsync();
}

// Swagger luôn bật ở Development; môi trường khác bật bằng cờ Swagger:Enabled (biến môi trường Swagger__Enabled=true).
// Không bật Swagger thì API vẫn public như cũ — bảo mật dựa vào [Authorize], không dựa vào giấu tài liệu.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
