using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Willow.Api.Client;
using Willow.Platform.Common;

namespace Willow.Http.DI
{
    public static class Extensions 
    {
        public static IServiceCollection SetHttpClient<T>(this IServiceCollection services, string name)
        {
            return services.SetHttpClient<T>(name, "", hasNamedM2MAuth: false);
        }

        public static IServiceCollection SetHttpClient<T>(this IServiceCollection services, string name, string authName, bool hasNamedM2MAuth)
        {
            services.AddHttpClient(name, (p, client) =>
            {
                var config = p.GetRequiredService<IConfiguration>();
                var logger = p.GetRequiredService<ILogger<T>>();

                client.BaseAddress = new Uri(config.Get($"{name}BaseAddress", logger));

                var namedM2MAuth = hasNamedM2MAuth ? name : string.Empty;

                string token = p.FetchMachineToMachineToken(authName, new ApiConfiguration
                {
                    ClientId     = config.Get($"{namedM2MAuth}M2MAuthClientId", logger),
                    ClientSecret = config.Get($"{namedM2MAuth}M2MAuthClientSecret", logger),
                    Audience     = config.Get($"{namedM2MAuth}M2MAuthAudience", logger, false),
                    UserName     = config.Get($"{namedM2MAuth}M2MAuthUserName", logger, false),
                    Password     = config.Get($"{namedM2MAuth}M2MAuthPassword", logger, false)

                }).Result;

                if (!string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            });

            return services;   
        }       

        public static IServiceCollection AddAuth0(this IServiceCollection services, string[]? namedAuth0Clients = null)
        {
            services.AddSingleton<IAuth0Cache>( p=> new Auth0Cache(new Platform.Common.MemoryCache(p.GetRequiredService<IMemoryCache>())));

            services.AddAuth0HttpClient();

            if (namedAuth0Clients == null)
            {
                return services;
            }

            foreach(var client in namedAuth0Clients)
            {
                services.AddAuth0HttpClient(true, client);
            }

            return services;   
        }        
        
        public static IServiceCollection AddAuth0HttpClient(this IServiceCollection services, bool hasNamedM2MAuth = false, string name = "")
        {
            var namedM2MAuth = hasNamedM2MAuth ? name : "";

            services.AddHttpClient($"Auth0{namedM2MAuth}", (p, client) =>
            {
                var config       = p.GetRequiredService<IConfiguration>();
                var logger       = p.GetRequiredService<ILogger<ServiceCollection>>();
                var domain       = config.Get($"{namedM2MAuth}M2MAuthDomain", logger, true);

                client.BaseAddress = new Uri($"https://{domain}");
            });

            return services;   
        }

        #region Private

        private static async Task<string> FetchMachineToMachineToken(this IServiceProvider services, string authName, ApiConfiguration config)
        {
            if(string.IsNullOrWhiteSpace(config.Password))
            {
                if(string.IsNullOrWhiteSpace(config.Audience))
                    throw new ArgumentNullException("config.Audience");   
            }
            else if(string.IsNullOrWhiteSpace(config.UserName))
                throw new ArgumentNullException("config.UserName");   

            authName = $"Auth0{authName}";

            var cache  = services.GetRequiredService<IAuth0Cache>();
            var logger = services.GetRequiredService<ILogger<ServiceCollection>>();
            var token  = await cache.Cache.Get<string>("MachineToMachineTokens_" + authName);

            if(token != null)
                return token;

            var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();
            var tokenService = new Auth0TokenService(new RestApi(httpClientFactory, authName));

            try
            { 
                var response = await tokenService.GetToken(config);

                try
                {
                    await cache.Cache.Add("MachineToMachineTokens_" + authName, 
                                          response.AccessToken,
                                          response.ExpiresIn.HasValue ? TimeSpan.FromSeconds(response.ExpiresIn.Value - 100) : TimeSpan.FromHours(1));
                }     
                catch (Exception ex)
                { 
                    logger.LogError(ex, ex.Message);
                }
            
                return response.AccessToken;
            }
            catch (Exception ex)
            { 
                logger.LogError(ex, ex.Message);
                throw new HttpRequestException($"Failed to get access token", ex);
            }
        }

        #endregion
    }
}