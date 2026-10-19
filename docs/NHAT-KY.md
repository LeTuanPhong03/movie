# 📓 Nhật ký học — Movie Reservation

> File này cập nhật **sau mỗi buổi học**. Mở project ở phiên mới thì đọc mục **Hôm nay học gì** trước.
> Lộ trình tổng thể và các ngày đã tick nằm ở [PLAN.md](PLAN.md).

## 📍 Hôm nay học gì

**Cập nhật lần cuối:** 19/10/2026

**Bước tiếp theo:**
1. **Bài 8 còn lại:** Register/Login/Refresh + validator xong. Còn: (7) `AddAuthentication` (JwtBearer, kiểm tra chữ ký/issuer/audience/hạn) + policy `AdminOnly` + nút Authorize trên Swagger → (8) seed admin từ config (mật khẩu ở user-secrets), endpoint thử chỉ Admin gọi được, kiểm tra 401 (không token) / 403 (token Customer). Sau đó tick PLAN 19/10
2. Tham khảo code Project 1 tại `D:\hoc\HocCS\E-Commerce-API\Ecommerce` (ValidationBehavior, LoginCommandValidator, cấu hình Swagger Authorize)

## 🔁 Cách ôn câu phỏng vấn

- Kho câu hỏi ở cuối file **không phải để trả lời hết ngay**, mà dùng để ôn dần.
- Cuối mỗi buổi, mentor hỏi **2–3 câu ngẫu nhiên** (ưu tiên câu cũ). Trả lời bằng lời của mình, không nhìn tài liệu.
- Câu trả lời sai hoặc ấp úng thì đánh dấu ⚠️ cạnh câu đó để hỏi lại hôm sau. Trả lời tốt 2 lần liền thì đánh dấu ✅.
- Gợi ý trong ngoặc chỉ để tự kiểm tra, **không học thuộc**.
- 17/10: ôn lần đầu cuối buổi. 3 câu: index leftmost prefix đúng; OnDelete và unique-vs-race còn thiếu ý (⚠️).
- 18/10 (cuối buổi): 3 câu — `user.Id` sau `Add` đúng; đăng ký `IAppDbContext` đúng ý (diễn đạt "request mới cùng luồng" còn lẫn — lỗi nằm **trong cùng 1 request**); extension method đúng ý (nhớ: không thật sự sửa class, `this` đánh dấu kiểu được mở rộng, cần `using`). Cả 3 đúng lần 1.
- 18/10 (đầu buổi): OnDelete đủ 2 ý (SetNull vì cột cho phép null + không xóa đơn để giữ lịch sử) → ✅. Kiểu khóa ngoại: đúng bước phát hiện (`migrations add`) nhưng chỉ nói "nhầm", chưa nêu luật. SHA-256 vs BCrypt: đúng vế mật khẩu, vế token nêu lý do phụ ("đổi nhiều lần") thay vì lý do chính (token ngẫu nhiên, không đoán được).
- 17/10 (phiên 2, cuối buổi): 3 câu ⚠️ trong ngày — 401/403 đủ 2 vế; defaultValue đúng ý chính (còn mơ hồ chữ "các dòng cũ"); Infrastructure đúng ý nhưng chưa nêu tên Dependency Inversion. Cả 3 đúng lần 1.
- 17/10 (phiên 2, đầu buổi): OnDelete — đặt SetNull **đúng chỗ** lần đầu, còn thiếu ý "thực tế không xóa đơn mà chuyển Cancelled". Unique-vs-race — nêu đúng race condition. Cả hai đúng lần 1, đúng thêm 1 lần nữa là ✅.

## 📅 Nhật ký

| Ngày | Bài | Đã học / đã làm | Còn yếu |
|---|---|---|---|
| 16/10 | Bài 1: ERD | 7 bảng và quan hệ; tách Seat/ShowtimeSeat; TotalPrice là snapshot; vòng đời ghế; viết `docs/decisions.md` | Hay nhầm tên trạng thái của **đơn** với của **ghế** |
| 16/10 | Dựng khung | Mentor tạo solution 6 project và thư mục; hiểu dependency rule (Api → Application → Domain); commit đầu tiên | |
| 17/10 | Bài 2: Enum | Viết `SeatStatus` và `ReservationStatus`; hiểu enum là gì; bài học "build không lỗi chưa chắc code đúng" | Lần đầu để `Expired` nhầm sang enum ghế (đã sửa); gõ sai tên `RevervationStatus` (đã sửa, commit `93e75be`) |
| 17/10 | Ôn ERD | Hiểu `ConfirmedAt`/`CancelledAt` lưu "lúc nào"; đơn Expired thì cả hai `null`; `ExpiresAt` đủ, không cần `ExpiredAt` | Trả lời đúng ngay lần đầu, nhầm lẫn trạng thái đã giảm |
| 17/10 | Bài 3: Entity | Tự viết đủ 7 entity; dùng `decimal` cho tiền, enum cho Status, `?` đúng chỗ. Học: khóa ngoại khác navigation property | Đặt tên chưa thống nhất (`ShowTime`/`Showtime`), lỗi chính tả, `Role` để string thay vì enum, `DurationMinutes` dùng `TimeSpan` |
| 17/10 | Bài 3: sửa review | Sửa 6/7 điểm; tự chọn tên `UserRole.Customer` (tốt hơn gợi ý); học warning CS8618 và cách `= string.Empty` | Sót `ShowTimeId` ở Reservation; dùng Ctrl+Shift+F để rà tên |
| 17/10 | Bài 3: xong ✅ | Build sạch 0 warning; `Genre` chọn `string` (cách A); commit `3b3cfcc` | Từng đổi `Genre` sang `int` để tắt cảnh báo → bài học: sửa cho đúng nghĩa, không sửa cho hết cảnh báo |
| 17/10 | Bài 4: Docker | Hiểu image / container / volume (laptop và ổ cứng gắn ngoài); viết `docker-compose.yml`; Postgres 17 chạy được | Từng nghĩ volume là "cache/backup", thật ra là nơi lưu dữ liệu thật, không phải bản sao |
| 17/10 | Bài 4: xong ✅ | Tự kiểm chứng volume giữ dữ liệu sau `down`/`up`; commit `cb728c1` | |
| 17/10 | Bài 5 phần 1 | Hiểu ORM / DbContext / DbSet / migration; cài EF Core 10 + Npgsql + `dotnet ef`; tự viết `AppDbContext` đủ 7 DbSet | Đặt tên kệ `ReservationSeats` cho `ShowtimeSeat` (tên DbSet = tên bảng) |
| 17/10 | Bài 5 phần 2: xong ✅ | Connection string, `AddDbContext` (DI), migration `InitialCreate`, 7 bảng trong Postgres; tự giải thích đúng `__EFMigrationsHistory` | Phát hiện qua `\d`: **chưa có khóa ngoại, chưa có unique index**, Status lưu dạng số → học ở Bài 6 |
| 17/10 | Commit Bài 5 | Commit `8fe5ded` | |
| 17/10 | Bài 6: Fluent API | Hiểu configuration = "nội quy" của bảng, đặt ở Infrastructure để Domain sạch; tự viết `ShowtimeSeatConfiguration` (lưu Status dạng chữ, 3 khóa ngoại, unique `(ShowtimeId, SeatId)`); `ApplyConfigurationsFromAssembly`; hiểu `virtual` / `override` / `base` | Unique chỉ `SeatId` sẽ làm ghế không bán được cho suất khác (không phải "ghi đè") |
| 17/10 | Bài 6: warning NuGet | Sửa MSB3277 (2 phiên bản EF Core) bằng cách ghi rõ EF Core 10.0.13 trong Infrastructure; build 0 warning | |
| 17/10 | Bài 6: config còn lại | Mentor viết 6 config còn lại theo yêu cầu. Học `OnDelete`: Cascade / Restrict / SetNull và vì sao Reservation dùng Restrict (giữ lịch sử doanh thu) | Gắn SetNull nhầm vào bảng Reservation (đúng là ở `ShowtimeSeat.ReservationId`) ⚠️ |
| 17/10 | Bài 6: index | Index = mục lục đã sắp xếp; đọc nhanh, ghi chậm; unique = index + luật; unique mạnh khi luật là "bằng nhau", luật khoảng thời gian (chồng lấn suất chiếu) phải kiểm tra bằng code. Tự quyết **không** unique `Showtimes(TheaterId, StartAt)`. Hiểu **leftmost prefix**, giải thích được vì sao EF không tạo index riêng cho `ShowtimeId` | Lúc đầu nói index `(TheaterId, StartAt)` giúp được `WHERE StartAt > now()` ⚠️ (sau đó đã trả lời đúng bài `Seats`) |
| 17/10 | Bài 6: xong ✅ | Migration `AddConfigurations` đã apply; `\d "ShowtimeSeats"` có FK + unique; thử insert trùng tên phòng → lỗi `IX_Theaters_Name`, ghế vào phòng 999 → lỗi `FK_Seats_Theaters_TheaterId`. Commit `3294dce` | Lỡ `git commit -am` thành 2 commit (`-a` bỏ qua file mới) → gộp bằng `git reset --soft HEAD~2`. Thói quen: `git add .` → `git status` → commit |
| 17/10 | Bài 7: Testcontainers | Hiểu vì sao không dùng EF InMemory (không có unique/FK/lock) và không dùng chung DB dev; `async`/`await`, `IAsyncLifetime`; tự viết 2 assert (`GetPendingMigrationsAsync` rỗng, `ShowtimeSeats` có 0 dòng). Test xanh lần đầu (~25s) | |
| 17/10 | Bài 7: xong ✅ | Thử làm test đỏ cố ý rồi sửa lại; commit `8f79c03`; **push lần đầu lên GitHub**. Giai đoạn 0 xong sớm 1 ngày | |
| 17/10 | Bài 8 bước 1: AuthN/AuthZ | Phân biệt Authentication (là ai → 401) và Authorization (được làm gì → 403); JWT = header.payload.signature, payload ai cũng đọc được, chữ ký chống sửa | Chỉ trả lời 403 mà quên trường hợp **không gửi token → 401** ⚠️ |
| 17/10 | Bài 8 bước 2: RegisterCommand | Cài MediatR 14.2.0; viết `RegisterCommand` dạng `record`; tự quyết **không cho chọn Role** khi đăng ký (Admin chỉ seed hoặc do admin khác cấp) | Lần đầu cho command trả về entity `User` → lộ `PasswordHash` ra API, phải trả `int` hoặc DTO |
| 17/10 | Bài 8: chốt luồng token | Người học quyết định làm **luồng chuẩn**: Register/Login trả access + refresh token, `/auth/refresh` cấp mới và xóa token cũ. Hiểu vì sao cần refresh token (access token không thu hồi được → hạn ngắn; refresh token lưu DB → thu hồi được) | |
| 17/10 | Bài 8 bước 2–3: review | `IPasswordHasher` đạt ngay; `AuthResponse` dùng `DateTime` (khớp các entity, Npgsql từ chối DateTime không phải UTC); hiểu tham số của positional record = property public → PascalCase | Lỗi chính tả lặp lại (`acessToken`, `comfirmPassword`) ⚠️; bọc `AuthRequest` trong `RegisterCommand` → JSON lồng 2 tầng, tên "AuthRequest" không hợp với Login |
| 17/10 | Bài 8 bước 2: xong ✅ | Sửa hết: xóa `AuthRequest`, command phẳng `(Email, Password, ConfirmPassword)`; đổi `ExpiresAt` → `AccessTokenExpiresAt` sau khi tự phát hiện tên mơ hồ (access và refresh có hạn khác nhau); build 0 warning | |
| 17/10 | Bài 8: thêm FullName | Người học tự đề xuất thêm tên; chốt `FullName` = tên hiển thị (trùng được, không unique), login vẫn bằng email. Học chuỗi việc khi đổi entity: Domain → Configuration → migration → Command | |
| 17/10 | Bài 8: FullName xong ✅ | Migration `AddUserFullName` đã apply, `\d "Users"` có `FullName varchar(100) not null default ''`. Học: EF suy NOT NULL từ `string` (không `?`); `defaultValue` để migration chạy được khi bảng đã có dữ liệu; mở psql bằng `docker exec -it movie-db psql ...` | Gõ thiếu `src/` trong `--project` → lỗi "Unable to retrieve project metadata" |
| 17/10 | Commit bước 2 + FullName | Commit `d1864e7`. Thảo luận: thư viện không "bắt buộc" cũng không "tối ưu nhất", chọn theo tiêu chí. MediatR (có thể tự viết, từ v13 cần license) và BCrypt (OWASP xếp Argon2id cao hơn, BCrypt vẫn chấp nhận được, giới hạn 72 byte) | |
| 17/10 | Bài 8 bước 3: xong ✅ | Cài BCrypt.Net-Next 4.2.0 vào Infrastructure; tự viết `BCryptPasswordHasher : IPasswordHasher`, build 0 warning | Chưa trả lời được "vì sao BCrypt ở Infrastructure" ⚠️ (Application chỉ khai báo *cần gì*, Infrastructure quyết định *làm bằng gì*) |
| 17/10 | Bài 8 bước 4: thiết kế RefreshTokens | Đặt đúng `ITokenService` (Application) / `JwtTokenService` + package JWT (Infrastructure); chọn Cascade khi xóa User, index cột token. Học: lưu **hash SHA-256** của token (token đã ngẫu nhiên đủ dài nên SHA-256 đủ, không cần BCrypt; BCrypt có salt nên không tra cứu được) | Thiếu cột `UserId`, `ExpiresAt`, `CreatedAt` ⚠️; nhầm "chuỗi ngẫu nhiên" (cách sinh) với "lưu token hay hash" (cách lưu) |
| 17/10 | Bài 8 bước 4a: review entity | Tự viết entity + config + DbSet, có Cascade và unique; **xin review trước khi migration** (thói quen tốt) | `UserId` để `string` trong khi `User.Id` là `int` ⚠️ (kiểu khóa ngoại phải khớp khóa chính); tên file `efreshTokenConfiguration.cs`; thiếu `HasMaxLength(64)`; tên `Token` thay vì `TokenHash` → **đã sửa hết** sau 1 lượt review |
| 17/10 | Bài 8 bước 4a: migration | Tạo `AddRefreshTokens`; đọc file thấy 2 `CreateIndex` (unique `TokenHash` tự viết + `UserId` do EF tự tạo cho khóa ngoại) và `onDelete: Cascade`. Học **convention** của EF (`Id` → khóa chính tự tăng, không cần `HasKey`) | |
| 17/10 | Bài 8 bước 4a: xong ✅ | `database update` thành công, `\d "RefreshTokens"` đủ 5 cột (`TokenHash varchar(64)`, thời gian `timestamptz`); 4 migration trong `__EFMigrationsHistory` | Docker Desktop hay tắt sau khi tắt máy → bật "Start when you sign in" |
| 18/10 | Bài 8 bước 4b: cấu hình | Phân biệt `appsettings.json` (chung mọi môi trường) và `appsettings.Development.json` (chỉ máy dev, ghi đè); thứ tự ghi đè json → Development → user-secrets → biến môi trường. `dotnet user-secrets init/set` lưu `Jwt:Key` ở `%APPDATA%\Microsoft\UserSecrets\<Id>` — **ngoài repo** để không thể commit/zip nhầm | |
| 18/10 | Bài 8 bước 4b: review ITokenService | Mục `Jwt` trong appsettings đúng (không có Key); user-secrets đã set. Bản đầu `ITokenService`: `GenerateToken(string userId, string role)` + `GenerateRefreshToken()`. Học cách thiết kế interface bằng cách **đi thử luồng của handler** xem còn thiếu gì | `userId` lại để `string` ⚠️; thiếu hạn access token cho `AuthResponse`; thiếu hash + hạn của refresh token để lưu DB |
| 18/10 | Bài 8 bước 4b: ITokenService ✅ | Sau khi đi thử luồng Register (5 thứ handler cần), tự thiết kế đủ: `AccessTokenResult(token, hạn)`, `RefreshTokenResult(gốc, hash, hạn, ngày tạo)`, `HashToken`, truyền `User` | Gõ sai `ExpiriesAt` ⚠️ (lỗi chính tả lặp lại) |
| 18/10 | Bài 8 bước 4b: JwtTokenService (dở) | Hiểu lỗi "no accessible extension method": `GetValue` là extension method nằm ở package `Microsoft.Extensions.Configuration.Binder` (Api có sẵn vì dùng Web SDK, Infrastructure thì không); positional record phải tạo bằng `( )`; `?? throw` kiểm tra chuỗi trước khi dùng. Quyết định: từ nay mọi file C# có XML doc `/// <summary>` | Gọi `GetUtcNow()` 2 lần → 2 mốc thời gian khác nhau; CS0161 do method chưa có `return` |
| 18/10 | Bài 8 bước 4b: xong ✅ | `JwtTokenService` đủ 3 method, `expiresAt` tính 1 lần dùng cho cả token và kết quả, refresh token 64 byte ngẫu nhiên, `GenerateRefreshToken` gọi lại `HashToken`; có XML summary | Hash dạng Base64 (44 ký tự) thay vì hex — vẫn đúng, nhưng đã chọn thì không đổi nữa vì hash cũ trong DB sẽ không khớp |
| 18/10 | Bài 8 bước 5: chốt cách truy cập DB | Commit `1caf83b`. So sánh Repository (cách Project 1) và `IAppDbContext`; người học chọn **`IAppDbContext`** vì project cần transaction và `UPDATE` có điều kiện cho concurrency, và để biết cả 2 cách | |
| 18/10 | Bài 8 bước 5a: IAppDbContext ✅ | Viết ADR mục 3 trong `decisions.md`; cài EF Core 10.0.13 vào Application; `IAppDbContext` đủ 8 `DbSet` (`{ get; }`) + `SaveChangesAsync`; `AppDbContext` implement được mà không viết thêm gì vì **kế thừa** `SaveChangesAsync` từ `DbContext`, và class có `{ get; set; }` vẫn thỏa interface chỉ đòi `{ get; }` | Chưa biết vì sao không cần viết `SaveChangesAsync` ⚠️; summary còn chung chung |
| 18/10 | Bài 8 bước 5b: extension method + lifetime | Hiểu extension method: `static class` + `static` method + `this` ở tham số đầu → gọi như method có sẵn (`builder.Services.AddInfrastructure(...)` = `DependencyInjection.AddInfrastructure(builder.Services, ...)`); dùng để gắn method vào kiểu mình không sở hữu; `AddDbContext`, `GetValue`, LINQ đều là extension method; cần `using` đúng namespace. `builder.Services` có kiểu `IServiceCollection` (sổ đăng ký DI). Lifetime Singleton / Scoped / Transient | Trước giờ dùng `AddDbContext`, `AddControllers` mà chưa để ý đó là extension method |
| 18/10 | Bài 8 bước 5b: DependencyInjection | Viết `AddInfrastructure` (DbContext, IAppDbContext, IPasswordHasher, ITokenService, TimeProvider). Hiểu "1 request = 1 scope": `AddScoped<IAppDbContext, AppDbContext>()` tạo **DbContext thứ 2** trong cùng request (mỗi mục đăng ký giữ object riêng) → Add qua cái này, SaveChanges qua cái kia thì mất dữ liệu, transaction không bọc hết, đọc dữ liệu cũ, tốn kết nối; sửa bằng `sp => sp.GetRequiredService<AppDbContext>()` (lấy cái đã có, không tạo mới) | Lần đầu viết dạng `<Interface, Class>` dù đã được dặn; summary dùng `//` thay vì `///` |
| 18/10 | Bài 8 bước 5c-1: AddApplication ✅ | Commit `7b8c5b4`. Viết `AddApplication` đăng ký MediatR bằng quét assembly (handler mới không cần đăng ký tay); hiểu vì sao mỗi tầng có file DI riêng (ai sở hữu thì người đó đăng ký; Program.cs chỉ gọi 2 dòng; Domain không có DI) | |
| 18/10 | Bài 8 bước 5c-2: chuẩn bị handler | `user.Id` = 0 sau `Add`, chỉ có giá trị sau `SaveChangesAsync` (Postgres sinh, EF đọc lại); lưu 2 lần = 2 transaction → dữ liệu dở dang → dùng **navigation property** `RefreshToken.User` để EF tự điền khóa ngoại trong 1 lần `SaveChanges`. Migration cho navigation **rỗng** (navigation chỉ có trong C#) → gỡ bằng `migrations remove` (chỉ khi chưa `database update`). Học **primary constructor** (C# 12) | Đặt tên property `user` viết thường; còn 2 dòng `Property(...)` rỗng |
| 18/10 | Bài 8 bước 5c-2: RegisterCommandHandler ✅ | Người học viết bản đầu; mentor sửa theo yêu cầu. Học: truyền record theo vị trí dễ đảo nhầm 2 tham số cùng kiểu (`Email`/`FullName` bị đảo, compiler không bắt) → dùng **named arguments**; API thiết kế cho FE dễ dùng → `AuthResponse` lồng `UserDto(Id, Email, FullName, Role)`; namespace phải khớp thư mục (`Common.Exceptions`) | Bản đầu **quên `Add`** → SaveChanges không lưu gì, Id = 0 ⚠️; quên `CreatedAt`, `Trim()`; summary ghi sai về primary constructor của class |
| 18/10 | Bài 8 bước 5c-3: AuthController + Swagger | Mentor viết theo yêu cầu: đổi `Microsoft.AspNetCore.OpenApi` → Swashbuckle 10.2.3 (Swagger UI chỉ bật ở Development), xóa WeatherForecast, `AuthController(ISender)` mỏng — chỉ `sender.Send`; file `.http` mẫu gọi register. Học: `( )` vs `{ }` khi `new`. Đã kiểm tra `/swagger` trả 200 | Chưa tự gọi register qua Swagger |
| 19/10 | Bài 8: test Register ✅ | Gọi register thành công, `RefreshTokens.UserId` khớp `Users.Id` (EF điền FK qua navigation). Đăng ký trùng → 500 (tự giải thích đúng: chưa có middleware, mọi exception → 500; sẽ đổi `ConflictException` → 409 ngày 28/10). Xóa `TimeProvider` → app **lỗi ngay lúc khởi động**: ở Development, ASP.NET bật `ValidateOnBuild` kiểm tra đủ DI khi `Build()` | |
| 19/10 | Bài 8: validator | Cài FluentValidation 12.1.1; viết `RegisterCommandValidator`; hiểu `ValidationBehavior` (generic cho mọi command, xin mọi `IValidator<TRequest>`, chạy hết, có lỗi thì ném `ValidationException`, không lỗi thì `next()` cho tới handler); file luật vs người kiểm tra; validator kiểm tra hình dạng (400), handler kiểm tra nghiệp vụ cần DB (409) | `ValidationBehavior` nhờ AI viết, chưa hiểu → đã giải thích từng dòng; **quên đăng ký** behavior + validator trong `AddApplication` ⚠️; lý do 72 ký tự nêu chưa đúng |
| 19/10 | Bài 8: Login | Tự viết `LoginCommand`/validator/handler/endpoint; báo lỗi chung chống user enumeration; Login không kiểm tra độ dài mật khẩu; 401 vs 403 áp dụng đúng (→ ✅); chọn named arguments (đảo thứ tự được) — biết thêm cách `required init` | Quên `using` EF (extension method); truyền `AuthResponse` sai vị trí + truyền entity thay `UserDto`; ném nhầm `UnauthorizedAccessException` (.NET); **có `Add` nhưng quên `SaveChangesAsync`** ⚠️ (ngược lỗi hôm qua); namespace lệch thư mục |
| 19/10 | Bài 8: Refresh + gom code ✅ | Người học trả lời 5 câu thiết kế rồi mentor code (theo yêu cầu tăng tốc): `AuthTokenIssuer` gom phần cấp token (AddRefreshToken → SaveChanges → CreateResponse) dùng cho Register/Login/Refresh; `RefreshTokenCommand` rotation (hash → tìm → kiểm tra hạn → Remove cũ + Add mới trong 1 SaveChanges); bắt `DbUpdateConcurrencyException` khi 2 request dùng cùng 1 token → 401. Test bằng curl: register, login email viết hoa, refresh 200, dùng lại token cũ bị từ chối, token mới dùng được; DB đúng 2 token | Câu 4: tưởng `/refresh` cần access token ⚠️ (gọi refresh chính là lúc access token hết hạn); câu 5: chưa biết 2 tab dùng chung 1 token → race |

## 🎤 Câu phỏng vấn đã luyện

- Vì sao tách `Seat` và `ShowtimeSeat`?
- Vì sao lưu `TotalPrice` thay vì tính lại từ `Showtime.Price`?
- Dependency rule trong Clean Architecture là gì?
- `ConfirmedAt` / `CancelledAt` dùng để làm gì? (doanh thu theo ngày lọc theo `ConfirmedAt`; chính sách hủy; tra soát khiếu nại)
- Vì sao dùng `decimal` cho tiền mà không dùng `double`? (double có lỗi làm tròn)
- Khóa ngoại (`ReservationId`) khác navigation property (`Reservation`) thế nào?
- Warning CS8618 nghĩa là gì, sửa thế nào? (property không-null nhưng chưa gán; `= string.Empty` hoặc `required`)
- Image, container, volume khác nhau thế nào? Vì sao không dùng `down -v` trên server?
- Vì sao không để mật khẩu trong code? (repo public ai cũng đọc được, bot quét GitHub; dùng biến môi trường)
- ORM / EF Core là gì? DbContext và migration là gì?
- Vì sao DbContext nằm ở Infrastructure chứ không ở Domain?
- Bảng `__EFMigrationsHistory` để làm gì? (ghi migration nào đã áp, để `update` chỉ chạy cái chưa áp)
- Dependency Injection là gì? (đăng ký 1 lần, framework tự tạo và đưa cho nơi cần)
- Vì sao không có cột `ExpiredAt`? (trùng với `ExpiresAt`; chỉ lưu thời điểm của sự kiện không đoán trước được)
- Fluent API configuration là gì? Vì sao không dùng attribute `[MaxLength]` trong Domain?
- Vì sao unique phải là cặp `(ShowtimeId, SeatId)` chứ không chỉ `SeatId`? (chỉ `SeatId` thì mỗi ghế chỉ được nằm trong 1 suất duy nhất)
- `virtual` / `override` / `base` khác nhau thế nào?
- ✅ (ôn 17/10: vẫn lẫn → phiên 2: đúng chỗ SetNull → 18/10: đủ ý) Cascade, Restrict, SetNull khác nhau thế nào? Reservation dùng gì, SetNull nằm ở bảng nào? (OnDelete mô tả chuyện xảy ra với **con**; SetNull ở `ShowtimeSeat.ReservationId` vì cột đó cho phép null; Restrict ở `Reservation.UserId/ShowtimeId` = không cho xóa User/Showtime đã có đơn; thực tế **không xóa đơn**, chỉ chuyển `Cancelled` để giữ doanh thu và nhả ghế đúng cách)
- Index là gì? Vì sao không đánh index lên mọi cột? (đọc nhanh nhưng ghi chậm, tốn chỗ)
- Unique index khác index thường thế nào? Khi nào unique không đủ? (luật khoảng thời gian, chồng lấn)
- ⚠️ (ôn 17/10: đúng lần 1, đúng thêm 1 lần nữa là ✅) Index `(A, B)` có giúp `WHERE B = ...` không? (không — leftmost prefix)
- ⚠️ (ôn 17/10: thiếu ý race condition → phiên 2: đúng lần 1) Vì sao code đã kiểm tra trùng rồi mà DB vẫn cần unique? (2 request cùng lúc đều kiểm tra "chưa có" → race condition)
- Integration test khác unit test thế nào? Vì sao dùng Testcontainers mà không dùng EF InMemory? (InMemory không có unique, FK, lock → test concurrency luôn pass dù code sai)
- Vì sao tên test nên dài, mô tả rõ? (test đỏ thì đọc tên là biết luật nào bị vỡ, không cần mở code)
- ✅ (17/10: quên vế 401 → cuối buổi đúng → 19/10 áp dụng đúng cho login sai = 401) Authentication khác Authorization thế nào? Khi nào trả 401, khi nào 403? (không có token/token sai → 401; có token hợp lệ nhưng không đủ quyền → 403)
- JWT gồm mấy phần? Vì sao không để `PasswordHash` (hay dữ liệu nhạy cảm) trong payload? (payload chỉ là base64, ai cầm token cũng đọc được; chữ ký đã chứng minh danh tính nên request thường không cần hash)
- Vì sao API đăng ký không cho client gửi `Role`? (client tự gửi `Admin` là leo quyền — privilege escalation; Admin chỉ seed hoặc do admin cấp)
- Vì sao không trả entity ra ngoài API? (lộ cột nhạy cảm như `PasswordHash`, client bị buộc chặt vào cấu trúc DB)
- Vì sao cần refresh token? Sao không cho access token sống 30 ngày luôn? (access token không thu hồi được vì server không lưu; refresh token lưu DB nên thu hồi được)
- Refresh token rotation là gì? (mỗi lần refresh thì cấp token mới và hủy token cũ → token bị lộ chỉ dùng được 1 lần)
- Vì sao hash mật khẩu bằng BCrypt mà không dùng SHA-256? (SHA-256 quá nhanh → brute-force hàng tỷ lần/giây; BCrypt cố ý chậm, có work factor tăng dần được, salt nằm sẵn trong hash)
- ⚠️ (17/10: thiếu "vì sao" → cuối buổi đúng ý, lần 1; nhớ nói rõ "các dòng **đã có sẵn**") Thêm cột NOT NULL vào bảng đã có dữ liệu thì sao? Vì sao EF sinh `defaultValue: ""`? (không có default thì các dòng cũ thành NULL → vi phạm NOT NULL → Postgres từ chối, migration hỏng)
- EF quyết định cột NULL hay NOT NULL dựa vào đâu? (`string` → NOT NULL, `string?` → NULL, nhờ nullable reference types) NOT NULL có chặn được chuỗi rỗng `""` không? (không → cần validator `NotEmpty`)
- Vì sao chọn MediatR / BCrypt? Có lựa chọn nào khác? (MediatR: tự gọi handler, `Mediator` source-generated, Wolverine; hash: Argon2id được OWASP ưu tiên, PBKDF2 có sẵn trong .NET; chọn BCrypt vì phổ biến, salt sẵn, đủ an toàn)
- ⚠️ (17/10: chưa trả lời được → cuối buổi đúng ý, lần 1; thiếu tên **Dependency Inversion**) Vì sao thư viện BCrypt / JWT nằm ở Infrastructure mà không ở Application? (Application chỉ khai báo interface — *cần gì*; Infrastructure quyết định *làm bằng gì*; đổi thư viện chỉ sửa Infrastructure, Application không biết)
- ⚠️ (18/10: nêu lý do phụ) Vì sao lưu hash của refresh token chứ không lưu nguyên chuỗi? Sao hash bằng SHA-256 mà mật khẩu lại phải BCrypt? (DB lộ thì hacker không dùng được token; token ngẫu nhiên 64 byte không đoán được nên hash nhanh vẫn an toàn, còn mật khẩu do người đặt nên dễ đoán → cần hash chậm; BCrypt có salt ngẫu nhiên nên không `WHERE TokenHash = ...` được)
- ⚠️ (18/10: chỉ nói "nhầm") Khóa ngoại khác kiểu khóa chính thì sao? Bị phát hiện ở bước nào? (FK phải cùng kiểu PK nó trỏ tới; build không bắt được, EF báo lỗi lúc `migrations add`)
- Vì sao dùng user-secrets mà không chỉ `.gitignore`? (file đã track thì ignore vô tác dụng; secret trong thư mục đi theo khi zip; lộ 1 lần là nằm mãi trong lịch sử git; user-secrets ở ngoài repo nên không thể commit nhầm, ASP.NET tự đọc ở Development)
- Thiết kế interface thế nào cho đúng? (đóng vai người dùng nó — handler — đi thử từng bước xem cần những gì; muốn trả nhiều giá trị thì gói vào record)
- Repository pattern và dùng DbContext trực tiếp khác nhau thế nào? Project này chọn gì, vì sao? (DbContext đã là repository + unit of work; cần transaction và `ExecuteUpdate` có điều kiện cho chống double-booking; đánh đổi: Application phụ thuộc EF Core)
- ⚠️ (18/10: chưa biết) `AppDbContext : DbContext, IAppDbContext` — vì sao không phải viết `SaveChangesAsync` mà vẫn build được? (kế thừa từ `DbContext` — method public có sẵn cũng tính là thỏa interface)
- (18/10 cuối buổi: đúng lần 1) Extension method là gì? Vì sao gọi được `builder.Services.AddDbContext(...)`? (static method trong static class, tham số đầu có `this`; C# dịch thành `Class.Method(builder.Services, ...)`; để gắn method vào kiểu không sở hữu; LINQ cũng là extension method)
- Singleton / Scoped / Transient khác nhau thế nào? DbContext dùng gì? (1 cho cả app / 1 mỗi request / mỗi lần xin; DbContext là Scoped)
- (18/10 cuối buổi: đúng lần 1, diễn đạt còn lẫn) Đăng ký `IAppDbContext` thế nào cho đúng? Vì sao không dùng `AddScoped<IAppDbContext, AppDbContext>()`? (dạng đó tạo DbContext thứ 2 trong cùng request; dùng factory `sp => sp.GetRequiredService<AppDbContext>()` để 2 tên trỏ về 1 object)
- (18/10 cuối buổi: đúng lần 1) Sau `Add(user)` thì `user.Id` bằng bao nhiêu? Khi nào có Id? Muốn lưu cha + con cùng lúc thì làm sao? (0; sau `SaveChanges` vì DB sinh; gắn navigation `child.Parent = parent`, EF tự điền FK trong 1 transaction)
- Navigation property có tạo cột trong DB không? (không — migration rỗng; chỉ khóa ngoại mới là cột)
- ⚠️ (18/10: quên `Add`; 19/10: quên `SaveChanges`) `Add` và `SaveChangesAsync` mỗi cái làm gì, thiếu một trong hai thì sao? Chỉ gọi `SaveChangesAsync` mà không `Add` thì sao? (DbContext không theo dõi object → không INSERT gì, không báo lỗi)
- Vì sao nên dùng named arguments khi tạo record nhiều tham số? (tham số cùng kiểu bị đảo thì compiler không bắt được)
- `new X(...)` khác `new X { ... }` thế nào? (`( )` đưa giá trị vào constructor lúc tạo; `{ }` = gọi constructor rỗng rồi gán từng property — quên property thì thành giá trị mặc định, compiler không nhắc; JSON ra giống nhau)
- Thiếu đăng ký DI thì lỗi lúc nào? (build C# vẫn qua; ở Development lỗi ngay khi `builder.Build()` nhờ `ValidateOnBuild`; môi trường khác có thể chỉ lỗi khi có request cần service đó)
- Pipeline behavior của MediatR là gì? `ValidationBehavior` hoạt động thế nào? (trạm chạy trước mọi handler; lấy mọi validator của request, có lỗi thì ném, không thì `next()`; viết 1 lần dùng cho mọi command)
- ⚠️ (19/10: nêu "nhanh hơn") Vì sao mật khẩu tối đa 72 ký tự? (BCrypt chỉ dùng 72 byte đầu, phần sau bị bỏ qua âm thầm)
- Vì sao DB đã giới hạn độ dài mà vẫn cần validator? (DB lỗi → 500 khó hiểu, báo từng lỗi một; validator → 400 rõ ràng, báo mọi lỗi cùng lúc, chặn trước khi tốn công hash)
- Login sai nên báo chung hay riêng "email không tồn tại" / "sai mật khẩu"? (chung — chống **user enumeration**; nâng cao: thời gian phản hồi cũng có thể lộ — timing attack; endpoint register báo "email đã tồn tại" là đánh đổi chấp nhận được)
- Vì sao validator của Login không kiểm tra độ dài/định dạng mật khẩu? (luật đăng ký có thể đổi; người dùng cũ với mật khẩu theo luật cũ sẽ bị khóa ngoài; Login chỉ cần so với hash)
- Client đã cầm token gốc thì hash refresh token trong DB để làm gì? (chống **lộ DB** — mất mọi tài khoản cùng lúc; trộm token từ client thì chống bằng lớp khác: hạn ngắn, rotation, thu hồi, cookie HttpOnly)
- ⚠️ (19/10: tưởng cần) `/auth/refresh` có cần access token không? (không — refresh được gọi đúng lúc access token hết hạn; bằng chứng danh tính là chính refresh token)
- Refresh token rotation khi 2 tab cùng gửi 1 token thì sao? (race: cả hai tìm thấy, bên lưu sau xóa dòng đã mất → `DbUpdateConcurrencyException` → trả 401; chỉ một bên thắng)
