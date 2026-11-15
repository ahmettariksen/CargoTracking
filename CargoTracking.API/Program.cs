using CargoTracking.API.Configuration;
using CargoTracking.API.Exceptions;
using CargoTracking.API.Initialization;
using CargoTracking.API.Services;
using CargoTracking.API.Swagger;
using CargoTracking.Application;
using CargoTracking.Application.Abstractions.Persistence;
using CargoTracking.Application.Abstractions.Persistence.Authentication;
using CargoTracking.Application.Interfaces;
using CargoTracking.Infrastructure.BackgroundJobs;
using CargoTracking.Infrastructure.Messaging;
using CargoTracking.Infrastructure.Messaging.Cunsomers;
using CargoTracking.Infrastructure.Options;
using CargoTracking.Infrastructure.Persistence.Context;
using CargoTracking.Infrastructure.Redis;
using CargoTracking.Infrastructure.Services;
using CargoTracking.Infrastructure.Services.Authentication;
using CargoTracking.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using StackExchange.Redis;
using System.Net;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<AdminOptions>()
    .Bind(builder.Configuration.GetSection(AdminOptions.SectionName))
    .Validate(x => !string.IsNullOrWhiteSpace(x.FirstName),
        "Initial admin first name is required.")
    .Validate(x => !string.IsNullOrWhiteSpace(x.LastName),
        "Initial admin last name is required.")
    .Validate(x => !string.IsNullOrWhiteSpace(x.Email),
        "Initial admin email is required.")
    .Validate(x => !string.IsNullOrWhiteSpace(x.Password),
        "Initial admin password is required.")
    .Validate(x => !string.IsNullOrWhiteSpace(x.PhoneNumber),
        "Initial admin phone number is required.")
    .ValidateOnStart();

builder.Services.AddScoped<AdminSeeder>();

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(x => !string.IsNullOrWhiteSpace(x.Issuer),
        "JWT Issuer is required.")
    .Validate(x => !string.IsNullOrWhiteSpace(x.Audience),
        "JWT Audience is required.")
    .Validate(x => !string.IsNullOrWhiteSpace(x.SecretKey)
        && System.Text.Encoding.UTF8.GetByteCount(x.SecretKey) >= 32,
        "JWT SecretKey must be at least 32 bytes.")
    .Validate(x => x.ExpirationInMinutes > 0,
        "JWT expiration must be greater than zero.")
    .ValidateOnStart();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = builder.Configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>()!;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,

            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();

builder.Services.AddDbContext<CargoTrackingDbContext>(options => options
    .UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// Outbox mesajlarýný SQL Server üzerinde atomik olarak sahiplenir.
builder.Services.AddScoped<
    IOutboxMessageClaimer,
    SqlServerOutboxMessageClaimer>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT token deðerini girin."

    });
    options.OperationFilter<AuthorizeOperationFilter>();
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddScoped<IApplicationDbContext, CargoTrackingDbContext>();
// Application assembly'sindeki CreateShipmentCommandHandler gibi handler'larý bulup Dependency Injection sistemine kaydedecek.
builder.Services.AddApplication();
builder.Services.AddScoped<IPasswordHasher, PasswordHasherService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddHttpContextAccessor(); //O anki HTTP isteðine ve isteði yapan kullanýcýnýn JWT ile doðrulanmýþ kimlik bilgilerine eriþmemizi saðlar.
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>(); //Her HTTP isteði için bu servisin kullanýlabilmesini saðlar.

//RabbitMQ DI üzerinden kaydý

// Aþaðýdaki kod ne yapar?
// Bind(...): appsettings.json içindeki RabbitMQ bölümünü RabbitMqOptions nesnesine baðlar.
// Validate(...): Host adý ve port deðerlerini kontrol eder.
// ValidateOnStart(): Ayarlar geçersizse uygulamanýn baþlangýçta hata vermesini saðlar.
// Þimdilik bu kod RabbitMQ'ya baðlanmaz; yalnýzca ayarlarý okur ve doðrular.
builder.Services
    .AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName))
    .Validate(x => !string.IsNullOrWhiteSpace(x.HostName),
    "RabbitMQ HostName is required.")
    .Validate(x => x.Port > 0 && x.Port <= 65535,
    "RabbitMQ Port is invalid.")
    .ValidateOnStart();

builder.Services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();

builder.Services.AddHostedService<OutboxBackgroundService>();

builder.Services.AddHostedService<ShipmentCreatedConsumer>();
builder.Services.AddHostedService<CourierAssignmentEmailConsumer>();

// builder.Configuration ile appsettings.json içindeki baðlantý adresini okuyor.
//ConnectionMultiplexer.Connect() ile Redis baðlantýsýný oluþturuyor.
//AddSingleton sayesinde uygulama boyunca ayný baðlantý nesnesini yeniden kullanýyor. Her istek için yeni Redis baðlantýsý açmýyoruz.builder.Configuration ile appsettings.json içindeki baðlantý adresini okuyor.
//ConnectionMultiplexer.Connect() ile Redis baðlantýsýný oluþturuyor.
//AddSingleton sayesinde uygulama boyunca ayný baðlantý nesnesini yeniden kullanýyor. Her istek için yeni Redis baðlantýsý açmýyoruz.
var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? throw new InvalidOperationException("Redis connection string is not configured.");
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
ConnectionMultiplexer.Connect(redisConnectionString));

//sql server ve redis baðlantýlarýnýn saðlýðýný kontrol etmemiz için gerekli ayarlamalar
builder.Services.AddHealthChecks().AddDbContextCheck<CargoTrackingDbContext>(
    name: "sqlserver").AddRedis(redisConnectionString, name: "redis");

builder.Services.AddScoped<ICourierLocationStore, RedisGeoService>();

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var adminSeeder = scope.ServiceProvider
        .GetRequiredService<AdminSeeder>();

    await adminSeeder.SeedAsync();
}

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.MapSwagger();
app.UseSwaggerUI();

app.Run();
