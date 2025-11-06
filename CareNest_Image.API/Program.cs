var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// CORS for dev/test: allow all origins with credentials
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// Cloudinary configuration via options and DI
// Ưu tiên đọc từ ENV, fallback sang appsettings (theo chuẩn deploy cloud)
// Đọc ENV trước, sau đó merge vào config để Options pattern hoạt động đúng
builder.Configuration["Cloudinary:CloudName"] = builder.Configuration["CLOUDINARY_CLOUD_NAME"] ?? builder.Configuration["Cloudinary:CloudName"] ?? string.Empty;
builder.Configuration["Cloudinary:ApiKey"] = builder.Configuration["CLOUDINARY_API_KEY"] ?? builder.Configuration["Cloudinary:ApiKey"] ?? string.Empty;
builder.Configuration["Cloudinary:ApiSecret"] = builder.Configuration["CLOUDINARY_API_SECRET"] ?? builder.Configuration["Cloudinary:ApiSecret"] ?? string.Empty;
if (!string.IsNullOrWhiteSpace(builder.Configuration["CLOUDINARY_UPLOAD_PRESET"]))
    builder.Configuration["Cloudinary:UploadPreset"] = builder.Configuration["CLOUDINARY_UPLOAD_PRESET"];
if (!string.IsNullOrWhiteSpace(builder.Configuration["CLOUDINARY_DEFAULT_FOLDER"]))
    builder.Configuration["Cloudinary:DefaultFolder"] = builder.Configuration["CLOUDINARY_DEFAULT_FOLDER"];

// Register Options pattern
builder.Services.Configure<CareNest_Image.API.Settings.CloudinaryOptions>(
    builder.Configuration.GetSection("Cloudinary"));

// Validate required config
var cloudinaryCloudName = builder.Configuration["Cloudinary:CloudName"];
var cloudinaryApiKey = builder.Configuration["Cloudinary:ApiKey"];
var cloudinaryApiSecret = builder.Configuration["Cloudinary:ApiSecret"];

if (string.IsNullOrWhiteSpace(cloudinaryCloudName))
{
    throw new InvalidOperationException("Cloudinary CloudName must be specified. Set CLOUDINARY_CLOUD_NAME environment variable or Cloudinary:CloudName in appsettings.");
}
if (string.IsNullOrWhiteSpace(cloudinaryApiKey))
{
    throw new InvalidOperationException("Cloudinary ApiKey must be specified. Set CLOUDINARY_API_KEY environment variable or Cloudinary:ApiKey in appsettings.");
}
if (string.IsNullOrWhiteSpace(cloudinaryApiSecret))
{
    throw new InvalidOperationException("Cloudinary ApiSecret must be specified. Set CLOUDINARY_API_SECRET environment variable or Cloudinary:ApiSecret in appsettings.");
}

// Register Cloudinary instance
builder.Services.AddSingleton(provider =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CareNest_Image.API.Settings.CloudinaryOptions>>().Value;
    var account = new CloudinaryDotNet.Account(options.CloudName, options.ApiKey, options.ApiSecret);
    var cloudinary = new CloudinaryDotNet.Cloudinary(account);
    cloudinary.Api.Secure = true;
    return cloudinary;
});
builder.Services.AddScoped<CareNest_Image.API.Services.IImageService, CareNest_Image.API.Services.CloudinaryImageService>();
builder.Services.AddSingleton<CareNest_Image.API.Repositories.IImageRepository, CareNest_Image.API.Repositories.InMemoryImageRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Log toàn bộ file và env giúp phát hiện thiếu config khi cloud run
foreach(var file in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory))
    Console.WriteLine($"File on container: {file}");
Console.WriteLine($"ASPNETCORE_ENVIRONMENT: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");

// Điều chỉnh bật Swagger theo env/config như checklist
var swaggerEnabled = app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled");
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();
