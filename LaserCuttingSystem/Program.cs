using LaserCuttingSystem.Core.Extensions;
using LaserCuttingSystem.Business.Extensions;
using LaserCuttingSystem.Data.Context;
using LaserCuttingSystem.Data.Extensions;
using LaserCuttingSystem.Data.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using LaserCuttingSystem.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

ApplyDevelopmentDefaults(builder);
ValidateRequiredSettings(builder.Configuration);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        // Refresh token cookie'si için credentials gerekir; bu AllowAnyOrigin ile birlikte kullanılamaz
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                             ?? new[] { "http://localhost:3000" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Swagger Configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Laser Cutting System API",
        Version = "v1",
        Description = "Makine Hata Kodu Yönetimi"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT token'ınızı giriniz. Örnek: Bearer {token}"
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
            Array.Empty<string>()
        }
    });
});

// Layer Extensions (order matters!)
builder.Services.AddCoreServices();
builder.Services.AddDataLayer(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddBusinessServices();

// Manual seeder registration
builder.Services.AddScoped<InitialDataSeeder>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    // Bekleyen migration'lar açılışta uygulanır (Database:AutoMigrate = false ile kapatılabilir)
    if (app.Configuration.GetValue("Database:AutoMigrate", true))
    {
        var db = scope.ServiceProvider.GetRequiredService<LaserCuttingDbContext>();
        await db.Database.MigrateAsync();
    }

    // Roller, abonelik planları ve ilk admin kullanıcısı
    var seeder = scope.ServiceProvider.GetRequiredService<InitialDataSeeder>();
    await seeder.SeedAsync();
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Laser Cutting System API v1");
        options.RoutePrefix = string.Empty;
    });
}


app.UseCors("AllowFrontend");

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Gizli değerler appsettings.json'da tutulmaz. Geliştirmede, ayarlanmamışlarsa projenin
// klonlanıp hemen çalıştırılabilmesi için yalnızca yerel kullanıma uygun varsayılanlar atanır.
static void ApplyDevelopmentDefaults(WebApplicationBuilder builder)
{
    if (!builder.Environment.IsDevelopment()) return;

    var config = builder.Configuration;
    void SetIfMissing(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(config[key])) config[key] = value;
    }

    SetIfMissing("JwtSettings:Key", "development-only-jwt-signing-key-do-not-use-in-production");
    SetIfMissing("JwtSettings:RequireHttpsMetadata", "false");
    SetIfMissing("SeedAdmin:Password", "Admin123!");
    // SMTP kapalıyken e-postalardaki bağlantılar (şifre sıfırlama, doğrulama) loga yazılır
    SetIfMissing("EmailSettings:LogWhenDisabled", "true");
}

static void ValidateRequiredSettings(IConfiguration config)
{
    var jwtKey = config["JwtSettings:Key"];
    if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    {
        throw new InvalidOperationException(
            "JwtSettings:Key ayarlanmamış veya 32 karakterden kısa. " +
            "User Secrets (dotnet user-secrets set \"JwtSettings:Key\" \"...\") ya da " +
            "JwtSettings__Key ortam değişkeni ile en az 32 karakterlik rastgele bir anahtar verin.");
    }
}
