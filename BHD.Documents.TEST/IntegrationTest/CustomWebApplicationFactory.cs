using System.Security.Claims;
using System.Text.Encodings.Web;
using Aplication.Services.Documents;
using Domain.Entities;
using Domain.Enums;
using Infraestructure.DbContext;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BHD.Documents.TEST.IntegrationTest;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<DataContext>));
            services.RemoveAll(typeof(DataContext));
            
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<DataContext>) ||
                           d.ServiceType == typeof(DataContext) ||
                           d.ServiceType.FullName?.Contains("DataContext") == true)
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<DataContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid().ToString());
            });

            services.RemoveAll(typeof(IDocumentUploadQueue));
            var mockUploadQueue = new Mock<IDocumentUploadQueue>();
            mockUploadQueue
                .Setup(q => q.EnqueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);
            mockUploadQueue
                .Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(string.Empty);
            services.AddSingleton(mockUploadQueue.Object);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

            var sp = services.BuildServiceProvider();

            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<DataContext>();

            db.Database.EnsureCreated();

            try
            {
                SeedTestData(db);
            }
            catch (Exception ex)
            {
                var logger = scopedServices.GetService<ILogger<CustomWebApplicationFactory<TProgram>>>();
                logger?.LogError(ex, "Error seeding test database");
            }
        });

        builder.UseEnvironment("Development");
    }

    private static void SeedTestData(DataContext context)
    {
        if (context.Documents.Any())
            return;

        var documents = new List<DocumentAsset>();

        for (int i = 1; i <= 35; i++)
        {
            documents.Add(new DocumentAsset
            {
                Id = Guid.NewGuid().ToString(),
                Filename = $"test_document_{i:D3}.pdf",
                ContentType = "application/pdf",
                DocumentType = (DocumentType)(i % 5),
                Channel = (Channel)(i % 4),
                CustomerId = $"CUST-{i:D5}",
                DocumentStatus = (DocumentStatus)(i % 3),
                Size = 1024 * i,
                UploadDate = DateTime.UtcNow.AddDays(-i),
                CorrelationId = $"CORR-{i:D5}",
                Url = $"https://storage.example.com/docs/{Guid.NewGuid()}"
            });
        }

        context.Documents.AddRange(documents);
        context.SaveChanges();
    }
}


public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.Fail("No Authorization header"));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "TestUser"),
            new Claim(ClaimTypes.Email, "test@bhd.com"),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
