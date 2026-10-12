using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;
using WatchStore.API.Middleware;
using WatchStore.API.Security;
using WatchStore.Core.Interfaces;
using WatchStore.Data.Context;
using WatchStore.Data.Repositories;
using WatchStore.Service.Profiles;
using WatchStore.Service.Security;
using WatchStore.Service.Services;

var logger = LogManager.Setup()
    .LoadConfigurationFromAppSettings()
    .GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // NLog מתחבר מתחת ל-ILogger הרגיל של .NET - הקוד בשכבות לא מכיר את NLog
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.Services.AddControllers()
        // enums כטקסט ("Pending" ולא 0) - גם בתשובות וגם בבקשות
        .AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
        .ConfigureApiBehaviorOptions(options =>
        {
            // שגיאות ולידציה של ה-DTOs (ModelState) באותו מבנה JSON כמו שאר השגיאות
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .ToDictionary(
                        e => e.Key,
                        e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray());

                return new BadRequestObjectResult(new
                {
                    status = 400,
                    message = "הנתונים שנשלחו אינם תקינים.",
                    errors,
                    correlationId = context.HttpContext.TraceIdentifier
                });
            };
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "WatchStore API", Version = "v1" });

        // כפתור Authorize ב-Swagger: מדביקים את ה-token שהתקבל מ-login
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    // DbContext הוא Scoped (ברירת המחדל): מופע אחד לכל בקשה.
    // לכן כל ה-repositories וה-services שתלויים בו גם Scoped -
    // singleton שמחזיק DbContext היה משתף אותו בין בקשות מקבילות (לא thread-safe)
    builder.Services.AddDbContext<WatchStoreDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is missing.")));

    // סורק את פרויקט ה-Service ורושם את כל ה-AutoMapper Profiles
    builder.Services.AddAutoMapper(typeof(ProductProfile).Assembly);

    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
    builder.Services.AddScoped<IStoreRepository, StoreRepository>();
    builder.Services.AddScoped<IOrderRepository, OrderRepository>();
    builder.Services.AddScoped<ITagRepository, TagRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();

    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddScoped<ICategoryService, CategoryService>();
    builder.Services.AddScoped<IStoreService, StoreService>();
    builder.Services.AddScoped<IOrderService, OrderService>();
    builder.Services.AddScoped<ITagService, TagService>();
    builder.Services.AddScoped<IAuthService, AuthService>();

    // ללא state - בטוח כ-singleton
    builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
    builder.Services.AddSingleton<ITokenService, TokenService>();

    // JWT: ההגדרות מ-appsettings, המפתח מ-User Secrets. נכשל בעלייה אם חסר
    builder.Services.AddOptions<JwtOptions>()
        .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
              ?? throw new InvalidOperationException("Jwt section is missing.");

    if (string.IsNullOrWhiteSpace(jwt.Key))
    {
        throw new InvalidOperationException(
            "Jwt:Key is missing. Run: dotnet user-secrets set \"Jwt:Key\" \"<32+ chars>\" --project WatchStore.API");
    }

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });

    builder.Services.AddAuthorization();

    builder.Services.AddCors(options =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? Array.Empty<string>();

        options.AddDefaultPolicy(policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders(CorrelationIdMiddleware.HeaderName));
    });

    var app = builder.Build();

    // מריץ את כל ה-migrations שעוד לא הורצו (כולל ה-seed),
    // כך שהמערכת עולה על מחשב חדש בלי התערבות ידנית בבסיס הנתונים
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider
            .GetRequiredService<WatchStoreDbContext>();

        dbContext.Database.Migrate();
    }

    // סדר ה-pipeline: טיפול בשגיאות ראשון (עוטף את כל השאר),
    // אחריו CorrelationId, ורק אז authentication ואחריו authorization
    app.UseMiddleware<ExceptionMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseCors();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    logger.Error(ex, "Application stopped because of an exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}
