using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using Willow.Api.Client;
using Willow.Data;
using Willow.Data.Rest;

namespace Willow.Data.Rest.DI
{
    public static class Extensions
    {
        public static IServiceCollection AddCachedRestRepository<TID, TVALUE>(this IServiceCollection services, string apiName, Func<TID, string> getEndPoint, Func<TID, string> getListEndpoint = null, int cacheDurationInHours = 1)
        {
            services.AddSingleton<IReadRepository<TID, TVALUE>>( (p)=> 
            {
                var cache = p.GetRequiredService<IMemoryCache>();
                var repo  = p.CreateRestRepository<TID, TVALUE>(apiName, getEndPoint, getListEndpoint);

                return new CachedRepository<TID, TVALUE>(repo, cache, TimeSpan.FromHours(cacheDurationInHours), nameof(TVALUE));
            });

            return services;
        }

        public static IReadRepository<TID, TVALUE> CreateRestRepository<TID, TVALUE>(this IServiceProvider p, string apiName, Func<TID, string> getEndPoint, Func<TID, string> getListEndpoint = null)
        {
            var api = p.CreateRestApi(apiName);
            
            return new RestRepositoryReader<TID, TVALUE>(api, getEndPoint, getListEndpoint);
        }

        public static IRestApi CreateRestApi(this IServiceProvider provider, string name)
        { 
            return new RestApi( provider.GetRequiredService<IHttpClientFactory>(), name);
        }    
    }
}