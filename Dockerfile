# Dockerfile multi-stage cho API.
# Stage 1 (sdk, ~800MB, có trình biên dịch) chỉ để build; stage 2 (aspnet, ~200MB) chỉ chứa kết quả publish
# → image nhỏ, deploy nhanh, không mang trình biên dịch/mã nguồn lên server (ít chỗ để khai thác).

# ---------- Stage 1: build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy riêng các file .csproj rồi restore trước: Docker cache theo từng lệnh,
# nên khi chỉ sửa code (không đổi package) thì bước restore (tải NuGet, chậm) được dùng lại từ cache.
COPY src/MovieReservation.Domain/MovieReservation.Domain.csproj src/MovieReservation.Domain/
COPY src/MovieReservation.Application/MovieReservation.Application.csproj src/MovieReservation.Application/
COPY src/MovieReservation.Infrastructure/MovieReservation.Infrastructure.csproj src/MovieReservation.Infrastructure/
COPY src/MovieReservation.Api/MovieReservation.Api.csproj src/MovieReservation.Api/
RUN dotnet restore src/MovieReservation.Api/MovieReservation.Api.csproj

COPY src/ src/
RUN dotnet publish src/MovieReservation.Api/MovieReservation.Api.csproj -c Release -o /app/publish --no-restore

# ---------- Stage 2: chạy ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Image .NET chạy app bằng user thường (không phải root) và nghe cổng 8080
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MovieReservation.Api.dll"]
