using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Willow.Api.AzureStorage;
using Willow.Common;
using Willow.Communications.Service;
using Willow.Data;
using Willow.Data.Rest.DI;
using Willow.Email.SendGrid;
using Willow.Functions.Common;
using Willow.Http.DI;
using Willow.Platform.Common;
using Willow.Platform.Models;
using Willow.Communications.Resolvers;
using Willow.PushNotification;

namespace Willow.Communications.Function
{
    public static class Extensions 
    {
        public static IServiceCollection AddCommSvc(this IServiceCollection services)
        {
            services.AddMemoryCache()
                    .AddHttpClient()
                    .AddAuth0()
                    .SetHttpClient<CommunicationsService>(ApiServiceNames.DirectoryCore)
                    .AddCachedRestRepository<Guid, Customer>(ApiServiceNames.DirectoryCore, (Guid id)=> $"customers/{id}", cacheDurationInHours: 4)
                    .AddSingleton<IRecipientResolver>( p=>
                    {
                        var userRepo = p.CreateRestRepository<UserRequest, User>(ApiServiceNames.DirectoryCore, (UserRequest request)=> $"users/{request.UserId}" + (request.UserType.HasValue ? $"?userType={(int)request.UserType}" : ""), null);

                        return new RecipientResolver(userRepo);
                    })
                    .AddSingleton<IPushNotificationService>(p => 
                    {
                        var config = p.GetRequiredService<IConfiguration>();
                        var logger = p.GetRequiredService<ILogger<PushNotificationService>>();

                        return new PushNotificationService(config.Get("PushNotificationConnectionString", logger), config.Get("PushNotificationHubPath", logger));
                    })
                    .AddSingleton<ICommunicationsService>( p=> 
                    {
                        var logger     = p.GetRequiredService<ILogger<CommunicationsService>>();

                        try
                        { 
                            var config     = p.GetRequiredService<IConfiguration>();
                            var pushNotify = p.GetRequiredService<IPushNotificationService>();
                            var sendGrid   = new SendGridEmailService(config.Get("SendGridApiKey", logger), 
                                                                      config.Get("EmailFromAddress", logger), 
                                                                      config.Get("EmailFromName", logger));
                            var hubs       = new Dictionary<string, INotificationHub> 
                            { 
                               { "email", sendGrid }
                              ,{ "pushnotification", pushNotify } 
                            };
                            var templateStore = p.CreateBlobStore<CommunicationsServiceFunction>(new BlobStorageConfig { AccountName   = config.Get("ContentStorageAccountName", logger), 
                                                                                                                         ContainerName = config.Get("ContentStorageContainerName", logger), 
                                                                                                                         AccountKey    = config.Get("ContentStorageAccountKey", logger, false)
                                                                                                                       },
                                                                                                                       "");
                            var recipientResolver = p.GetRequiredService<IRecipientResolver>(); 
                            var customerRepo = p.GetRequiredService<IReadRepository<Guid, Customer>>(); 

                            return new CommunicationsService(hubs, templateStore, recipientResolver, customerRepo, logger);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(new Exception("Unable to create communication service", ex), ex.Message);
                            throw;
                        }
                     });  

            return services;   
        }
    }
}