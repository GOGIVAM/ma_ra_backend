using System.Text;
using System.Threading.RateLimiting;
using MaRa.Api.Data;
using MaRa.Api.Models.Entities;
using MaRa.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

// ── Bootstrap logger (avant DI) ───────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, _, cfg) =>
    {
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .Enrich.FromLogContext()
           .Enrich.WithMachineName();

        // Sink PostgreSQL optionnel (activé si la table audit_logs est disponible)
        var connStr = ctx.Configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connStr))
        {
            try
            {
                var cols = new Dictionary<string, Serilog.Sinks.PostgreSQL.ColumnWriters.ColumnWriterBase>
                {
                    { "message",    new Serilog.Sinks.PostgreSQL.ColumnWriters.RenderedMessageColumnWriter() },
                    { "level",      new Serilog.Sinks.PostgreSQL.ColumnWriters.LevelColumnWriter() },
                    { "timestamp",  new Serilog.Sinks.PostgreSQL.ColumnWriters.TimestampColumnWriter() },
                    { "properties", new Serilog.Sinks.PostgreSQL.ColumnWriters.PropertiesColumnWriter() },
                };
                cfg.WriteTo.PostgreSQL(connStr, "audit_logs",
                    columnOptions: cols,
                    needAutoCreateTable: true);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Serilog sink PostgreSQL non configuré — journalisation console uniquement");
            }
        }
    });

    // ── Base de données ───────────────────────────────────────────────────────
    builder.Services.AddDbContext<AppDbContext>(opts =>
        opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
            .UseSnakeCaseNamingConvention());

    // ── ASP.NET Core Identity (CDC §3.5.1) ────────────────────────────────────
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(opts =>
    {
        opts.Password.RequireDigit        = true;
        opts.Password.RequiredLength      = 8;
        opts.Password.RequireUppercase    = false;
        opts.Password.RequireNonAlphanumeric = false;
        // Verrouillage après 5 tentatives — CDC §3.5.4
        opts.Lockout.MaxFailedAccessAttempts = 5;
        opts.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(15);
        opts.Lockout.AllowedForNewUsers      = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

    // ── JWT Bearer (RFC 7519 — CDC §3.5.1) ───────────────────────────────────
    var jwtCfg    = builder.Configuration.GetSection("Jwt");
    var secretKey = jwtCfg["SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey manquante");

    builder.Services.AddAuthentication(opts =>
    {
        opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        opts.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtCfg["Issuer"],
            ValidAudience            = jwtCfg["Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew                = TimeSpan.Zero,
        };
    });

    // ── Politiques d'autorisation par groupe CAMRAIL ──────────────────────────
    builder.Services.AddAuthorization(opts =>
    {
        opts.AddPolicy("AdminOnly",  p => p.RequireClaim("groupe", "ADMIN"));
        opts.AddPolicy("DMAT",       p => p.RequireClaim("groupe", "DMAT",       "ADMIN"));
        opts.AddPolicy("DIF",        p => p.RequireClaim("groupe", "DIF",        "ADMIN"));
        opts.AddPolicy("CI",         p => p.RequireClaim("groupe", "CI", "CIF",  "ADMIN"));
        opts.AddPolicy("Technicien", p => p.RequireClaim("groupe",
            "DMAT", "DIF", "CI", "CIF", "ADMIN"));
    });

    // ── Rate Limiting — protection force brute (CDC §3.5.4) ──────────────────
    builder.Services.AddRateLimiter(opts =>
    {
        // 10 tentatives par minute sur le endpoint /auth/login
        opts.AddFixedWindowLimiter("auth", o =>
        {
            o.PermitLimit            = 10;
            o.Window                 = TimeSpan.FromMinutes(1);
            o.QueueProcessingOrder   = QueueProcessingOrder.OldestFirst;
            o.QueueLimit             = 0;
        });
        opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    // ── Services applicatifs ──────────────────────────────────────────────────
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IAuditService, AuditService>();
    builder.Services.AddScoped<IDocumentService, DocumentService>();

    builder.Services.AddHttpClient<IFastApiService, FastApiService>(client =>
    {
        var baseUrl = builder.Configuration["FastApi:BaseUrl"] ?? "http://localhost:8000";
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout     = TimeSpan.FromSeconds(30);
    });

    // ── CORS (CDC §3.1 — tablettes, postes fixes) ────────────────────────────
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["*"];
    builder.Services.AddCors(opts =>
        opts.AddDefaultPolicy(policy =>
        {
            if (origins.Contains("*"))
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            else
                policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }));

    // ── Controllers + Swagger ─────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(opts =>
    {
        opts.SwaggerDoc("v1", new OpenApiInfo
        {
            Title       = "MA-RA Backend .NET — CAMRAIL",
            Version     = "v1",
            Description = "Backend applicatif : auth, équipements, gammes, documents, proxy IA (CDC §3.2)"
        });
        var bearerScheme = new OpenApiSecurityScheme
        {
            Type         = SecuritySchemeType.Http,
            Scheme       = "bearer",
            BearerFormat = "JWT",
            Description  = "JWT obtenu via POST /api/auth/login"
        };
        opts.AddSecurityDefinition("Bearer", bearerScheme);
        opts.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    // ── Upload de fichiers ────────────────────────────────────────────────────
    builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(opts =>
    {
        opts.MultipartBodyLengthLimit = 52_428_800; // 50 Mo
    });

    // ─────────────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── Migrations + seed de démarrage ───────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        Log.Information("Migrations appliquées");

        // Seed uniquement si la variable d'environnement SEED_DB=true
        // ou si on est en développement (prototype)
        var seedEnv = scope.ServiceProvider.GetRequiredService<IConfiguration>()["SeedDb"];
        var env     = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (seedEnv?.ToLower() == "true" || env.IsDevelopment())
        {
            await DbInitializer.SeedAsync(scope.ServiceProvider);
            Log.Information("Seed base de données terminé");
        }
    }

    // ── Pipeline middleware ────────────────────────────────────────────────────
    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} en {Elapsed:0}ms";
    });

    // En-têtes de sécurité (CDC §3.5.3 / §3.5.4)
    app.Use(async (ctx, next) =>
    {
        ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        ctx.Response.Headers.Append("X-Frame-Options",        "DENY");
        ctx.Response.Headers.Append("Referrer-Policy",        "no-referrer");
        ctx.Response.Headers.Append("Content-Security-Policy",
            "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'");
        await next();
    });

    app.UseHttpsRedirection();
    app.UseHsts();
    app.UseStaticFiles();
    app.UseCors();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(opts => opts.SwaggerEndpoint("/swagger/v1/swagger.json", "MA-RA v1"));
    }

    app.MapControllers();

    Log.Information("MA-RA Backend .NET démarré (env={Env})", app.Environment.EnvironmentName);
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Arrêt inattendu de MA-RA Backend");
}
finally
{
    await Log.CloseAndFlushAsync();
}
