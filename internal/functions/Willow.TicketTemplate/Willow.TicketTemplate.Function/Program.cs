using System.Net.Http;
using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Willow.Api.Client;
using Willow.Http.DI;
using Willow.Functions.Common;

namespace Willow.TicketTemplate.Function
{
    public class Program
    {
        public static Microsoft.Extensions.Configuration.IConfigurationRoot Configuration { get; set; }

        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureAppConfiguration((context, config) =>
                {
                    Configuration = config
                                  #if DEBUG
                                    .SetBasePath(Directory.GetCurrentDirectory())
                                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                                  #endif
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
                     .AddMemoryCache()
                     .AddHttpClient()
                     .AddAuth0()
                     .SetHttpClient<TicketTemplateFunction>(ApiServiceNames.DirectoryCore)
                     .SetHttpClient<TicketTemplateFunction>(ApiServiceNames.WorkflowCore)
                     .AddScoped<ITicketTemplateService>( p=> 
                     {
                        var directoryCore = new RestApi(p.GetRequiredService<IHttpClientFactory>(), ApiServiceNames.DirectoryCore);
                        var workflowCore  = new RestApi(p.GetRequiredService<IHttpClientFactory>(), ApiServiceNames.WorkflowCore);

                        return new TicketTemplateService(directoryCore, workflowCore);
                     });
                })
                .Build();

            host.Run();
        }
    }
}
