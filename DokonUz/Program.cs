using DokonUz.Data;
using DokonUz.Helpers;
using DokonUz.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 📌 CORS sozlamalari
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 📌 Kestrel server sozlamalari (HTTP va HTTPS portlar)
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000); // HTTP
    options.ListenAnyIP(5001, listenOptions =>
    {
        listenOptions.UseHttps(); // HTTPS qo‘llab-quvvatlash
    });
});

// 📌 Serilog log konfiguratsiyasi
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog(); // Serilogni qo‘shish

// 📌 PaymentSettings sozlamalarini yuklash
var paymentSettingsSection = builder.Configuration.GetSection("PaymentSettings");
builder.Services.Configure<PaymentSettings>(paymentSettingsSection);

// 📌 Swagger konfiguratsiyasi
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "E-Commerce API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT tokeningizni quyidagi formatda kiriting: Bearer [token]"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// 📌 Bazaga ulanish (SQL Server)
builder.Services.AddDbContext<DokonUzDbContext>(options =>
    options.UseLazyLoadingProxies()
           .UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 📌 AuthSettings sozlamalarini yuklash
var authSettingsSection = builder.Configuration.GetSection("AuthSettings");
var authSettings = authSettingsSection.Get<AuthSettings>();

if (authSettings == null || string.IsNullOrEmpty(authSettings.Secret))
{
    throw new Exception("AuthSettings.Secret is not configured properly in appsettings.json.");
}

builder.Services.Configure<AuthSettings>(authSettingsSection);

// 📌 JWT autentifikatsiya konfiguratsiyasi
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authSettings.Secret)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

// 📌 JSON opsiyalarini sozlash
builder.Services.AddControllers()
       .AddJsonOptions(options =>
       {
           options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
       });

// 📌 API versiyalash
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});

// 📌 AutoMapper sozlash
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// 📌 Ilovani yaratish
var app = builder.Build();

// 📌 Swagger UI ni ishga tushirish
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "E-Commerce API v1.0");
});

// 📌 Middleware va xavfsizlik
app.UseSerilogRequestLogging(); // HTTP so‘rovlarini loglash
app.UseMiddleware<ErrorHandlerMiddleware>(); // Xatoliklarni ushlash uchun middleware

app.UseStaticFiles();
app.UseCors("AllowAll"); // CORS ni yoqish
app.UseHttpsRedirection();
app.UseAuthentication(); // JWT autentifikatsiyani qo‘shish
app.UseAuthorization();

app.MapControllers();
app.Run();
