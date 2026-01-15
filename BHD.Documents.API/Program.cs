using System.Text;
using System.Text.Json.Serialization;
using Aplication.Interfaces.Services;
using Aplication.Interfaces.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Infraestructure.DbContext;
using Aplication.Services.Documents;
using Domain.Repositories;
using Infraestructure.Repositories;
using Aplication.Services.Documents.Publisher;
using BHD.Filters;
using Infraestructure.BackgroundServices;
using Infraestructure.Publisher;
using Infraestructure.Services.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IDocumentPublisher, InMemoryDocumentPublisher>();
builder.Services.AddHostedService<DocumentUploadProcessor>();
builder.Services.AddSingleton<IDocumentUploadQueue, DocumentUploadQueue>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddSwaggerGen(options =>
{

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header usando el esquema Bearer. 
                      Ingresa 'Bearer' [espacio] y luego tu token. 
                      Ejemplo: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
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
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });

    options.UseInlineDefinitionsForEnums();
});


var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Secret"];

if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException(
        "JWT Secret no está configurado. Por favor, agrega 'Jwt:Secret' en appsettings.json");
}

if (secretKey.Length < 32)
{
    throw new InvalidOperationException(
        $"JWT Secret debe tener al menos 32 caracteres. Actualmente tiene: {secretKey.Length}");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false; 
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BHD Document Asset Gateway",
        Version = "v1.0.0",
        Description = "API para gestión de documentos con procesamiento asíncrono",
        Contact = new OpenApiContact
        {
            Email = "noreply@bhd.com.do"
        }
    });
    options.OperationFilter<FileUploadOperationFilter>();
    options.UseInlineDefinitionsForEnums();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<DataContext>();
    
    var maxRetries = 15;
    var delay = TimeSpan.FromSeconds(5);
    
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            logger.LogInformation("Attempting database migration... (Attempt {Attempt}/{Max})", i + 1, maxRetries);
            // context.Database.Migrate();
            logger.LogInformation("Database migration completed successfully.");
            break;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Database not ready: {Message}. Retrying in {Delay}s...", ex.Message, delay.TotalSeconds);
            
            if (i == maxRetries - 1)
            {
                logger.LogError("Could not connect to database after {Max} attempts.", maxRetries);
                throw;
            }
            
            Thread.Sleep(delay);
        }
    }
}
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
    dbContext.Database.Migrate();
}
if (!app.Environment.IsEnvironment("Test"))
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        dbContext.Database.Migrate();
    }
}
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }