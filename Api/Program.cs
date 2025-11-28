using Api.Configuration;
using Api.Middleware;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;

// Make Program class public for integration tests
public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            CreateApplication(args).Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            // Se ejecuta cuando la app se detiene
            Log.CloseAndFlush();  
        }
    }

    public static WebApplication CreateApplication(string[] args)
    {
        try
        {
            var builder = WebApplication.CreateBuilder(args);

            if (builder.Environment.IsDevelopment())
            {
                // Cargar User-Secrets solo en Development (archivo encriptado local, NO en Git)
                builder.Configuration.AddUserSecrets<Program>(optional: true);
                Log.Information("User-Secrets cargados para Development");
            }

            // Cargar Environment Variables (siempre, máxima prioridad en Azure)
            builder.Configuration.AddEnvironmentVariables();
            Log.Information("Environment Variables cargados");

            // Application Insights Configuration
            var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
            if (!string.IsNullOrEmpty(appInsightsConnectionString))
            {
                builder.Services.AddApplicationInsightsTelemetry(options =>
                {
                    options.ConnectionString = appInsightsConnectionString;
                });
                Log.Information("Application Insights initialized with connection string");
            }
            else if (builder.Environment.IsProduction())
            {
                Log.Warning("Application Insights ConnectionString is missing in Production environment");
            }

            // Serilog Configuration - Initialize early for startup logging
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();

            builder.Host.UseSerilog(Log.Logger);  // Usar Serilog como logger de ASP.NET Core

            Log.Information("Starting Hexagonal Architecture Template application");
            Log.Information("WebApplication.CreateBuilder completed successfully");

            //// CORS Configuration OLD (Version permisiva)
            //string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
            //var corsPolicies = builder.Configuration.GetSection("CorsPolicies").Get<List<CorsPolicyConfig>>() ?? new List<CorsPolicyConfig>();
            //builder.Services.AddCors(options =>
            //{
            //    options.AddPolicy(name: MyAllowSpecificOrigins,
            //                      builder =>
            //                      {
            //                          // Version totalmente permisiva
            //                          builder.AllowAnyOrigin()
            //                                 .AllowAnyHeader()
            //                                 .AllowAnyMethod();
            //                      });
            //});
            //Log.Information("CORS policy configured: {PolicyName}", MyAllowSpecificOrigins);


            // CORS Configuration
            string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
            var corsPolicies = builder.Configuration.GetSection("CorsPolicies").Get<List<CorsPolicyConfig>>() ?? new List<CorsPolicyConfig>();
            var isDevelopment = builder.Environment.IsDevelopment();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: MyAllowSpecificOrigins,
                                  builder =>
                                  {
                                      if (isDevelopment)
                                      {
                                          // Ambiente LOCAL: Muy permisivo para desarrollo
                                          builder.AllowAnyOrigin()
                                                 .AllowAnyHeader()
                                                 .AllowAnyMethod();
                                          Log.Information("CORS configured in DEVELOPMENT mode: AllowAnyOrigin");
                                      }
                                      else
                                      {
                                          // Ambiente PRODUCCIÓN (Azure): Restrictivo con dominios específicos
                                          if (corsPolicies.Count > 0)
                                          {
                                              var allowedOrigins = corsPolicies.Select(p => p.Origin).ToArray();
                                              builder.WithOrigins(allowedOrigins)
                                                     .AllowAnyHeader()
                                                     .AllowAnyMethod()
                                                     .AllowCredentials();

                                              Log.Information("CORS configured in PRODUCTION mode with {OriginCount} allowed origins: {Origins}",
                                                  allowedOrigins.Length, string.Join(", ", allowedOrigins));
                                          }
                                          else
                                          {
                                              Log.Warning("No CORS policies defined for Production environment. CORS disabled for security.");
                                          }
                                      }
                                  });
            });

            Log.Information("CORS policy '{PolicyName}' registered", MyAllowSpecificOrigins);

            // JWT Authentication Configuration
            Log.Information("Configuring JWT Authentication...");
            var jwtSettings = builder.Configuration.GetSection("Authentication");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];

            if (string.IsNullOrEmpty(secretKey))
            {
                Log.Fatal("JWT SecretKey is missing or empty in configuration");
                throw new ArgumentNullException("Authentication:SecretKey", "JWT Secret Key cannot be empty");
            }

            if (string.IsNullOrEmpty(issuer))
                Log.Warning("JWT Issuer is missing in configuration");

            if (string.IsNullOrEmpty(audience))
                Log.Warning("JWT Audience is missing in configuration");

            Log.Information("JWT Configuration loaded - Issuer: {Issuer}, Audience: {Audience}", issuer, audience);

            // Configure Authentication with JWT Bearer
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false; // Only for development
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            // Configure Authorization
            builder.Services.AddAuthorization();
            Log.Information("JWT Authentication and Authorization configured successfully");

            // MVC and API Explorer Configuration
            builder.Services.AddMvc();
            builder.Services.AddEndpointsApiExplorer();
            Log.Information("MVC and API Explorer services configured");

            // Swagger Configuration
            Log.Information("Configuring Swagger documentation...");
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = builder.Configuration["SwaggerDoc:Version"] ?? "v1",
                    Title = builder.Configuration["SwaggerDoc:Title"] ?? "API",
                    Description = builder.Configuration["SwaggerDoc:Description"] ?? "API Description",
                    Contact = new OpenApiContact
                    {
                        Name = "Contact the developer"
                    },
                    License = new OpenApiLicense
                    {
                        Name = "License",
                    }
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter 'Bearer' [space] and then your valid JWT token.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
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

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                    Log.Information("XML documentation found and included: {XmlPath}", xmlPath);
                }
                else
                {
                    Log.Warning("XML documentation not found at: {XmlPath}", xmlPath);
                }
            });
            Log.Information("Swagger documentation configured successfully");

            // Database Configuration
            Log.Information("Configuring database connection...");
            var connectionString = builder.Configuration.GetConnectionString("dbContext");

            if (string.IsNullOrEmpty(connectionString))
            {
                Log.Fatal("Database connection string 'dbContext' is missing or empty");
                throw new ArgumentNullException("ConnectionStrings:dbContext", "Database connection string cannot be empty");
            }

            builder.Services.AddSqlServer<AppDbContext>(connectionString);
            Log.Information("Database context configured successfully");

            // Dependency Injection
            Log.Information("Registering application dependencies...");
            builder.Services.RegisterDependencies();
            Log.Information("Application dependencies registered successfully");

            // AutoMapper configuration
            builder.Services.AddAutoMapper(typeof(Infrastructure.Mapping.AutoMapperProfile));
            Log.Information("AutoMapper configured successfully");

            builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });
            Log.Information("Controllers and JSON options configured");

            // Rate Limiting Configuration usando Fixed Window Limiter
            builder.Services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("Fixed", limiterOptions =>
                {
                    limiterOptions.PermitLimit = 100; // Maximo 100 requests
                    limiterOptions.Window = TimeSpan.FromMinutes(1); // Por minuto
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = 0; // No encola solicitudes
                });
            });
            Log.Information("Rate limiting configured");

            // Build Application
            Log.Information("Building application...");
            var app = builder.Build();
            Log.Information("Application built successfully");

            // Initialize ResourceTextHelper with configuration
            Application.Helpers.ResourceTextHelper.Initialize(app.Configuration);
            Log.Information("ResourceTextHelper initialized with localization settings");

            // Configure HTTP Pipeline
            Log.Information("Configuring HTTP request pipeline...");

            // Security Headers Middleware (debe ir primero para que se aplique a todas las respuestas)
            app.UseMiddleware<SecurityHeadersMiddleware>();

            // Security Audit Middleware (para logging de eventos sospechosos)
            app.UseMiddleware<SecurityAuditMiddleware>();

            // Global Exception Handling Middleware
            app.UseMiddleware<GlobalExceptionMiddleware>();

            // Token Blacklist Middleware(valida tokens revocados)
            app.UseMiddleware<TokenBlacklistMiddleware>();

            // Request Logging Middleware (despues del GlobalExceptionMiddleware)
            app.UseMiddleware<RequestLoggingMiddleware>();

            // Rate Limiting Middleware
            app.UseRateLimiter();

            // Enable CORS
            app.UseCors(MyAllowSpecificOrigins);
            Log.Debug("CORS middleware applied");

            // Enable Swagger
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Hexagonal Architecture Template API v1");
                options.RoutePrefix = "swagger";
            });
            Log.Information("Swagger UI configured at /swagger endpoint");

            // HTTPS Redirection and Routing
            app.UseHttpsRedirection();
            app.UseRouting();
            Log.Debug("HTTPS redirection and routing middleware configured");

            // Authentication & Authorization
            app.UseAuthentication();
            app.UseAuthorization();
            Log.Debug("Authentication and authorization middleware configured");

            // Map Controllers
            app.MapControllers();
            Log.Information("Controllers mapped successfully");

            Log.Information("Starting web server...");
            Log.Information("Swagger UI available at: https://localhost:7085/swagger/index.html");

            Log.Information("---------------------------------------------------");
            Log.Information("AMBIENTE ACTIVO: {Environment}", app.Environment.EnvironmentName);
            Log.Information("---------------------------------------------------");

            if (app.Environment.IsDevelopment())
            {
                Log.Information("Modo: DEVELOPMENT (Local)");
                Log.Information("BD: SQL Server Local (localhost)");
                Log.Information("Email: Configuración de desarrollo");
                Log.Information("CORS: Permisivo (localhost:3000, localhost:4200)");
                Log.Information("Rate Limit: 1000 req/s (relajado)");
                Log.Information("Secrets: User-Secrets + appsettings.Development.json");
            }
            else if (app.Environment.IsProduction())
            {
                Log.Information("Modo: PRODUCTION (Azure)");
                Log.Information("BD: Azure SQL Database");
                Log.Information("Email: Configuración de producción");
                Log.Information("CORS: Restrictivo (dominios específicos)");
                Log.Information("Rate Limit: 100 req/s (estricto)");
                Log.Information("Secrets: Environment Variables (Azure)");
            }
            else
            {
                Log.Warning("Ambiente no reconocido: {Environment}", app.Environment.EnvironmentName);
                Log.Warning("Solo se soportan: Development y Production");
            }

            var displayConnectionString = app.Configuration.GetConnectionString("dbContext");
            if (!string.IsNullOrEmpty(displayConnectionString))
            {
                var displayString = displayConnectionString.Length > 60 
                    ? displayConnectionString.Substring(0, 60) + "..." 
                    : displayConnectionString;
                Log.Information("Connection String: {ConnectionString}", displayString);
            }

            Log.Information("---------------------------------------------------");

            return app;
        }
        catch (Exception ex)
        {
            // Catch startup exceptions
            Log.Fatal(ex, "Application failed to start due to a critical error");
            throw;
        }
    }
}