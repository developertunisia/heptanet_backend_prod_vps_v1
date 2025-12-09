using HeptaNet.API.Middleware;
using HeptaNet.API.Services;
using HeptaNet.Application.Services;
using HeptaNet.Application.Services.Audio;
using HeptaNet.Application.Services.Email;
using HeptaNet.Application.Services.FileStorage;
using HeptaNet.Application.Services.Otp;
using HeptaNet.Application.Settings;
using HeptaNet.Domain.Base;
using HeptaNet.Domain.Constants;
using HeptaNet.Domain.Models;
using HeptaNet.Infrastructure.Audio;
using HeptaNet.Infrastructure.Database;
using HeptaNet.Infrastructure.Email;
using HeptaNet.Infrastructure.FileStorage;
using HeptaNet.Infrastructure.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);



// ==================== CONFIGURATION CORS ====================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFlutterWeb",
        policy =>
        {
            // Autorise toutes les origines localhost avec n'importe quel port
            policy.SetIsOriginAllowed(origin =>
            {
                // Pour le développement local
                if (builder.Environment.IsDevelopment())
                {
                     // Allow localhost with any port (for web browsers)
                    if (origin?.StartsWith("http://localhost:") ?? false)
                        return true;
                    
                    // Allow local network IP (for Android physical devices)
                    if (origin?.StartsWith("http://192.168.1.9:") ?? false)
                        return true;
                    
                    // Allow any local network IP (more permissive for development)
                    if (origin?.StartsWith("http://192.168.") ?? false)
                        return true;
                    
                    // Allow 10.0.2.2 (Android emulator's special alias for host machine)
                    if (origin?.StartsWith("http://10.0.2.2:") ?? false)
                        return true;
                    
                    // Allow null origin (for mobile apps making direct HTTP requests)
                    if (string.IsNullOrEmpty(origin))
                        return true;
                }

                // Permet les applications mobiles / desktop (origin peut être null)
                if (string.IsNullOrEmpty(origin)) return true;

                // Pour la production, listez vos origines spécifiques
                string[] allowedOrigins = {
                  "http://51.83.72.29",     
                    "https://www.yourdomain.com"
                };
                return allowedOrigins.Contains(origin);
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });
});

// Add DbContext with PostgreSQL
builder.Services.AddDbContext<MyAppContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("HeptaNet.Infrastructure")));

// Configure Identity (sans cookies)
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;

    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<MyAppContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey!)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        // ✅ Mapper correctement les claims
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role
    };
    
    // ✅ Événements pour debug (optionnel mais utile)
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var claims = context.Principal?.Claims.Select(c => $"{c.Type}: {c.Value}");
            Console.WriteLine($"Token validated. Claims: {string.Join(", ", claims ?? Array.Empty<string>())}");
            return Task.CompletedTask;
        },
        // ✅ Support for SignalR authentication
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            // If the request is for SignalR hub and token is in query string
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
            {
                context.Token = accessToken;
            }
            
            return Task.CompletedTask;
        }
    };
});

// ==================== CONFIGURATION AUTHORIZATION POLICIES ====================
builder.Services.AddAuthorization(options =>
{
    // Politique pour SuperAdmin uniquement
    options.AddPolicy("SuperAdminOnly", policy =>
        policy.RequireRole(AppRoles.SuperAdmin));
    
    // Politique pour Admin et SuperAdmin
    options.AddPolicy("AdminAccess", policy =>
        policy.RequireRole(AppRoles.SuperAdmin, AppRoles.Admin));
    
    // Politique pour la gestion des emails autorisés (SuperAdmin uniquement)
    options.AddPolicy("ManageAuthorizedEmails", policy =>
        policy.RequireRole(AppRoles.SuperAdmin));
    
    // Politique pour voir les emails autorisés (Admin et SuperAdmin)
    options.AddPolicy("ViewAuthorizedEmails", policy =>
        policy.RequireRole(AppRoles.SuperAdmin, AppRoles.Admin));
    
    // Politique personnalisée: utilisateur non blacklisté
    options.AddPolicy("NotBlacklisted", policy =>
        policy.RequireAssertion(context =>
        {
            var isBlacklisted = context.User.FindFirst("IsBlacklisted")?.Value;
            return isBlacklisted == null || isBlacklisted.ToLower() != "true";
        }));
});

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<OtpSettings>(builder.Configuration.GetSection("Otp"));

// Register repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthorizedEmailRepository, AuthorizedEmailRepository>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IOtpRepository, OtpRepository>();

// Register file storage and audio services
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IAudioMetadataService, AudioMetadataService>();


// Register services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Register messaging services
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IConversationService, ConversationService>();

// Register connection tracking as singleton (shared across all requests)
builder.Services.AddSingleton<IConnectionTrackingService, ConnectionTrackingService>();


// Add controllers with JSON serialization in camelCase
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddScoped<IAuthorizedEmailService, AuthorizedEmailService>();
builder.Services.AddScoped<IOtpService, OtpService>();

// ==================== CONFIGURATION SIGNALR ====================
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.MaximumReceiveMessageSize = 102400; // 100 KB
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "HeptaNet API",
        Version = "v1",
        Description = "API pour HeptaNet avec authentification JWT"
    });

    // Configuration pour le support JWT dans Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


var app = builder.Build();
// Seed roles et super admin
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var dbContext = services.GetRequiredService<MyAppContext>();

        var maxRetries = 10;
        var delay = TimeSpan.FromSeconds(5);

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                Console.WriteLine("⏳ Applying migrations...");
                dbContext.Database.Migrate();
                Console.WriteLine("✅ Migrations applied successfully.");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Migration failed: {ex.Message}");

                if (i == maxRetries - 1)
                    throw;

                Console.WriteLine("🔄 Retrying in 5 seconds...");
                await Task.Delay(delay);
            }
        }

        // SEEDING
        await HeptaNet.Infrastructure.Database.DataSeeder.SeedRolesAsync(services);
        await HeptaNet.Infrastructure.Database.DataSeeder.SeedSuperAdminAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Erreur lors de la migration ou du seeding.");
    }
}


// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ⚠️ IMPORTANT: Désactiver HTTPS redirection en développement pour éviter les erreurs CORS
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Enable static files for serving uploaded files
//app.UseStaticFiles();
string uploadsPath;
var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var isProduction = !string.IsNullOrEmpty(env) && 
                 (env.Equals("Production", StringComparison.OrdinalIgnoreCase) ||
                  env.Equals("Prod", StringComparison.OrdinalIgnoreCase));
var isDocker = System.IO.File.Exists("/.dockerenv");

if (isProduction || isDocker)
{
    // En production Docker, utiliser le chemin du volume monté
    uploadsPath = "/app/HeptaNet.API/uploads";
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Production/Docker mode: Using /app/HeptaNet.API/uploads for static files");
}
else
{
    // En développement, utiliser le chemin relatif
    uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation($"Development mode: Using {uploadsPath} for static files");
}

if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});


// ⚠️ IMPORTANT: Ajouter CORS AVANT Authentication/Authorization
app.UseCors("AllowFlutterWeb");
app.UseAuthentication();
app.UseAuthorization();

// Global exception handler middleware (doit être après UseRouting mais avant MapControllers)
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.MapControllers();

// ==================== MAP SIGNALR HUB ====================
app.MapHub<HeptaNet.API.Hubs.ChatHub>("/hubs/chat");

// Before app.Run()
app.Urls.Add("http://0.0.0.0:5106");
app.Run();

