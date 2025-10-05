using Hangfire;
using Hangfire.Storage.SQLite;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Resend;
using Serilog;
using System.Text;
using Tienda.src.Application.Jobs;
using Tienda.src.Application.Mappers;
using Tienda.src.Application.Services.Implements;
using Tienda.src.Application.Services.Interfaces;
using Tienda.src.Domain.Models;
using Tienda.src.Infrastructure.Data;
using Tienda.src.Infrastructure.Repositories.Implements;
using Tienda.src.Infrastructure.Repositories.Interfaces;


var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("SqliteDatabase") ?? throw new InvalidOperationException("Connection string SqliteDatabase no configurado");

// Configuración de Mapster
MapperExtensions.ConfigureMapster();



# region Logging Configuration
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services));
#endregion

// ---------- Servicios ----------
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IVerificationCodeRepository, VerificationCodeRepository>();
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IFileService, FileService>();
// Services (auth)
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// Products
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();


// ==== Resend  ====    
builder.Services.AddOptions();
builder.Services.AddHttpClient<ResendClient>();
builder.Services.Configure<ResendClientOptions>(o =>
{
    o.ApiToken = builder.Configuration["ResendAPIKey"]
                 ?? throw new InvalidOperationException("ResendAPIKey no está configurado.");
});
builder.Services.AddTransient<IResend, ResendClient>();
builder.Services.AddScoped<IEmailService, EmailService>();

#region Database Configuration
Log.Information("Configurando base de datos SQLite");
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlite(connectionString));
#endregion

// Identity 
#region Identity Configuration
Log.Information("Configurando Identity");
builder.Services.AddDataProtection();
builder.Services.AddIdentityCore<User>(options =>
{
    //Configuración de contraseña
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;

    //Configuración de Email
    options.User.RequireUniqueEmail = true;

    //Configuración de UserName
    options.User.AllowedUserNameCharacters = builder.Configuration["IdentityConfiguration:AllowedUserNameCharacters"] ?? throw new InvalidOperationException("Los caracteres permitidos para UserName no están configurados.");
})
.AddRoles<Role>()
.AddEntityFrameworkStores<DataContext>()
.AddDefaultTokenProviders();
#endregion

#region Authentication Configuration
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    string jwtSecret = builder.Configuration["JWTSecret"]
        ?? throw new InvalidOperationException("La clave secreta JWT no está configurada.");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization();
#endregion

#region Hangfire Configuration
Log.Information("Configurando Hangfire");
var cronExpression = builder.Configuration["Jobs:CronJobsDeleteUnconfirmedUsers"]
    ?? throw new InvalidOperationException("La expresión CRON para eliminar usuarios no confirmados no está configurada.");
var timeZone = TimeZoneInfo.FindSystemTimeZoneById(
    builder.Configuration["Jobs:TimeZone"]
    ?? throw new InvalidOperationException("La zona horaria para los trabajos no está configurada.")
);
builder.Services.AddHangfire(config =>
{
    var connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);
    var databasePath = connectionStringBuilder.DataSource;
    config.UseSQLiteStorage(databasePath);
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170);
    config.UseSimpleAssemblyNameTypeSerializer();
    config.UseRecommendedSerializerSettings();
});
builder.Services.AddHangfireServer();
#endregion
// ---------- App ----------
var app = builder.Build();


// === Hangfire Dashboard (leer de la sección correcta y con mayúscula F) ===
var dashboardSection = builder.Configuration.GetSection("HangFireDashboard");

// Lee el path desde la sección correcta
var dashboardPath = dashboardSection.GetValue<string>("DashboardPath")
    ?? throw new InvalidOperationException("La ruta para el dashboard de Hangfire no está configurada (HangFireDashboard:DashboardPath).");

// Debe ser un *path* que empiece con "/"
if (!dashboardPath.StartsWith("/"))
{
    throw new InvalidOperationException("HangFireDashboard:DashboardPath debe comenzar con '/'. Ejemplo: '/hangfire'. No uses host/puerto.");
}

// Lee opciones desde la misma sección (ojo con la F mayúscula)
var dashboardOptions = new DashboardOptions
{
    StatsPollingInterval = dashboardSection.GetValue<int?>("StatsPollingInterval") ?? 5000,
    DashboardTitle = dashboardSection.GetValue<string>("DashboardTitle") ?? "Hangfire",
    DisplayStorageConnectionString = dashboardSection.GetValue<bool?>("DisplayStorageConnectionString") ?? false
};

app.UseHangfireDashboard(dashboardPath, dashboardOptions);

#region Database Migration and jobs Configuration
Log.Information("Aplicando migraciones pendientes y configurando trabajos programados");
using (var scope = app.Services.CreateScope())
{
    await DataSeeder.Initialize(scope.ServiceProvider);
    var jobId = nameof(UserJob.DeleteUnconfirmedAsync);
    RecurringJob.AddOrUpdate<UserJob>
        (jobId,
         job => job.DeleteUnconfirmedAsync(),
        cronExpression,
        new RecurringJobOptions
        {
            TimeZone = timeZone
        });

    Log.Information("Job recurrente: {JobId} con expresión CRON: {CronExpression} en zona horaria: {TimeZone}", jobId, cronExpression, timeZone);

}
#endregion


app.MapControllers();
app.MapOpenApi();

// Ejecutar seeder
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await DataSeeder.Initialize(services);
}

app.Run();