using System.IO;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Willow.Functions.Common;

namespace Willow.Communications.Function
{
    public class Program
    {
        public static IConfigurationRoot Configuration { get; set; }

        public static async Task Main()
        {       
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureAppConfiguration((context, config) =>
                {
                    Configuration = config
                                  #if DEBUG
                                    .SetBasePath(Directory.GetCurrentDirectory())
                                    .AddJsonFile("local.settings.json", optional: false, reloadOnChange: true)
                                  #endif
                                    .AddEnvironmentVariables()
                                    .Build();
                    var keyVaultName = Configuration.GetValue<string>("Azure:KeyVault:KeyVaultName");
                    if (!string.IsNullOrEmpty(keyVaultName))
                    {
                        Configuration = config.AddPrefixedKeyVault(
                            keyVaultName,
                            new[] { "CommSvc", "Functions", "Common" }
                        ).Build();
                    }
                    config.AddConfiguration(Configuration);
                })
                .ConfigureServices(s =>  
                {
                    s.AddLogging()
                     .AddCommSvc();
                })

                .Build();

            await host.RunAsync();
        }
    }
}
