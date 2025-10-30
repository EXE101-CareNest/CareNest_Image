# HƯỚNG DẪN TRIỂN KHAI .NET API + PostgreSQL LÊN KOYEB (CHUẨN DÙNG LẠI)

File này hướng dẫn bạn chuẩn hoá quy trình để deploy bất cứ dịch vụ .NET Web API nào + DB PostgreSQL lên Koyeb cho mọi dự án.

---

## 1. Chuẩn bị dự án
- Đảm bảo `Dockerfile` dạng multi-stage build (xem mẫu dưới).
- Trong code, database configuration **phải ưu tiên đọc từ biến môi trường**: `DB_HOST`, `DB_PORT`, `DB_USER`, `DB_PASSWORD`, `DB_NAME` (đầy đủ, không dùng hard code connection string fix cứng trong code).
- Tạo sẵn file `appsettings.Production.json` (giá trị mặc định); khi deploy thực tế, chỉ lấy từ env.

## 2. Dockerfile mẫu
```dockerfile
# build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore <tên_solution>.sln
RUN dotnet publish <đường_dẫn_cproj>/<tên_api_project>.csproj -c Release -o /app /p:UseAppHost=false

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "<tên_api_project>.dll"]
```
## 3. Đọc config DB chuẩn cloud
Ví dụ lấy từ env, fallback sang appsettings:
```csharp
var config = builder.Configuration;
DatabaseSettings dbSettings = new DatabaseSettings
{
    Ip       = config["DB_HOST"] ?? config["DatabaseSettings:Ip"],
    Port     = int.TryParse(config["DB_PORT"], out var port) ? port : (config.GetSection("DatabaseSettings").GetValue<int?>("Port") ?? 5432),
    User     = config["DB_USER"] ?? config["DatabaseSettings:User"],
    Password = config["DB_PASSWORD"] ?? config["DatabaseSettings:Password"],
    Database = config["DB_NAME"] ?? config["DatabaseSettings:Database"]
};
```
- Khi deploy local, dùng appsettings. Khi deploy cloud thì Koyeb/Docker sẽ inject env trực tiếp.

## 4. Đăng ký DbContext
```csharp
builder.Services.AddDbContext<DatabaseContext>(options =>
    options.UseNpgsql(
        connectionString + ";Pooling=true;Maximum Pool Size=5;Minimum Pool Size=0;Timeout=15;",
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
            // npgsqlOptions.CommandTimeout(60);
        }));
```

## 5. Hướng dẫn migrate (tốt nhất):
- Chỉ migrate khi biến môi trường `RUN_MIGRATIONS=true`.
- Sau đó phải về lại `RUN_MIGRATIONS=false` (tránh migrate lặp lại khi scaling hoặc lỗi).
```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    var runMigrations = Environment.GetEnvironmentVariable("RUN_MIGRATIONS");
    if (!string.IsNullOrWhiteSpace(runMigrations) && runMigrations.Equals("true", StringComparison.OrdinalIgnoreCase))
    {
        context.Database.Migrate();
    }
}
```
## 6. Swagger: Nên bật bằng cả env/config
```csharp
var swaggerEnabled = app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled");
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

## 7. Cấu hình env trên Koyeb
Đặt các biến môi trường này khi tạo dịch vụ:
| Tên             | Giá trị                                                             |
|-----------------|---------------------------------------------------------------------|
| ASPNETCORE_ENVIRONMENT | Production                                                     |
| ASPNETCORE_URLS | http://0.0.0.0:8080                                                |
| DB_HOST         | <host name hoặc endpoint postgresql>                                |
| DB_PORT         | 5432                                                                |
| DB_NAME         | <tên database>                                                      |
| DB_USER         | <tên user>                                                          |
| DB_PASSWORD     | <mật khẩu user>                                                     |
| RUN_MIGRATIONS  | false (hoặc true khi migrate; sau migrate đổi lại về false)         |
| Swagger:Enabled | true                                                                |

**Health check:**
- Port: 8080
- HTTP path: /swagger (hoặc /health nếu có)

## 8. Quy trình migrate production
1. Đặt `RUN_MIGRATIONS=true`   [33m→ redeploy [0m
2. Xem log xác nhận migrate thành công.
3. Đổi lại `RUN_MIGRATIONS=false`  [33m→ redeploy lại [0m

## 9. Các lỗi thường gặp
- Nếu log hiện: `IP: ${DB_HOST}` → Chưa đọc đúng env, hãy kiểm tra lại code lấy config DB như HƯỚNG DẪN.
- Port health check fail (8000): cần phải Expose/cấu hình đúng port 8080 và health check trên port 8080 của Docker/Koyeb/app.
- Too many connections: giảm pool size.

---
**Bạn cầm file này cho dự án .NET API nào cũng dùng được, chỉ cần đổi đúng tên solution, tên csproj file và setup theo bảng env trên là thành công!**

---

## 10. Checklist chống lỗi mất cấu hình khi deploy cloud

- **Phải luôn có file** `appsettings.Production.json` nằm trong thư mục project chính, đầy đủ các key như DatabaseSettings, APIService (BaseUrl...), Swagger...
- Dockerfile có lệnh:
  ```dockerfile
  COPY --from=publish /app/publish .
  ```
  đảm bảo mọi file json (kể cả appsettings.Production.json) được copy vào image.
- **ASPNETCORE_ENVIRONMENT luôn là "Production"** (chính xác phân biệt hoa/thường), app sẽ pick đúng file cấu hình.
- Nếu gặp lỗi config bị rỗng (service baseurl, swagger v.v):
  - Thêm đoạn log này ở đầu Program.cs:
    ```csharp
    foreach (var file in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory))
        Console.WriteLine(">>> File on container: " + file);
    Console.WriteLine(">>> ASPNETCORE_ENVIRONMENT: " + Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
    ```
  - Sau đó kiểm tra log khi app chạy trên cloud, phải thấy dòng hiện có appsettings.Production.json và đúng môi trường Production.
- Không cần set ENV cho các baseurl khi đã có trong file, chỉ đặt ENV nếu muốn override key bất kỳ.
- Nếu cloud không cho phép ENV có dấu ":", dùng hai dấu "__" (ví dụ: Swagger__Enabled).
- Luôn kiểm tra lại các giá trị config thực tế qua log khi start container, nhất là trước/giữa quá trình fix lỗi!

---

**Làm đúng như checklist cuối cùng này, chắc chắn deployment không bao giờ gặp lỗi load thiếu cấu hình json hoặc thiếu baseurl, swagger ở cloud nữa.**