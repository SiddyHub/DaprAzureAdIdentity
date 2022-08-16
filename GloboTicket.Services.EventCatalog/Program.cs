using Dapr.Client;
using Dapr.Extensions.Configuration;
using GloboTicket.Services.EventCatalog.DbContexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;

namespace GloboTicket.Services.EventCatalog
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {                
                var dbCosmos = scope.ServiceProvider.GetRequiredService<EventCatalogCosmosDbContext>();

                //dbCosmos.Database.EnsureDeleted();
                dbCosmos.Database.EnsureCreated();
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(config => 
                {
                    var daprClient = new DaprClientBuilder().Build();                  
                    var secretDescriptors = new List<DaprSecretDescriptor>
                    {
                        new DaprSecretDescriptor("CosmosDb:Endpoint"),
                        new DaprSecretDescriptor("CosmosDb:Key"),
                        new DaprSecretDescriptor("CosmosDb:DatabaseName")
                    };
                    config.AddDaprSecretStore("secretstore", secretDescriptors, daprClient);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
