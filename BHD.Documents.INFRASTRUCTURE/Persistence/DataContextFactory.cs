using Infraestructure.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infraestructure.Persistence;

public class DataContextFactory : IDesignTimeDbContextFactory<DbContext.DataContext>
{
   
   public DbContext.DataContext CreateDbContext(string[] args)
   {

      var apiPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "BHD.Documents.API");
      var configuration = new ConfigurationBuilder()
         .SetBasePath(apiPath)
         .AddJsonFile("appsettings.json", optional: false)
         .AddJsonFile("appsettings.Development.json", optional: true)
         .Build();
      var connectionString = configuration.GetConnectionString("DefaultConnection")
                             ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' no encontrado");
      var optionsBuilder = new DbContextOptionsBuilder<DbContext.DataContext>();
      optionsBuilder.UseSqlServer(connectionString);
      return new DbContext.DataContext(optionsBuilder.Options);
   }
}
