using System.Text;

using Microsoft.Extensions.Caching.Distributed;
using Redis = Microsoft.Extensions.Caching.Redis;
using Newtonsoft.Json;

using Willow.Common;

namespace Willow.Azure.Cache
{
    public class RedisCache : ICache
    {
        private readonly IDistributedCache _cache;

        public RedisCache(string connectionString)
        {
            _cache = new Redis.RedisCache(new Redis.RedisCacheOptions
            {
                Configuration = connectionString
            });
        }

        #region ICache

        public Task Add<T>(string key, T objToAdd) where T : class
        {
            if(typeof(T) == typeof(string))
            {
                return _cache.SetStringAsync(key, objToAdd?.ToString()!);
            }

            return _cache.SetStringAsync(key, JsonConvert.SerializeObject(objToAdd));
        }

        public Task Add<T>(string key, T objToAdd, DateTime dtExpires) where T : class
        {
            if(typeof(T) == typeof(string))
            {
                return _cache.SetStringAsync(key, objToAdd?.ToString()!, new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = dtExpires
                });
            }

            return _cache.SetStringAsync(key, JsonConvert.SerializeObject(objToAdd), new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = dtExpires
            });
        }

        public Task Add<T>(string key, T objToAdd, TimeSpan tsExpires) where T : class
        {
           if(typeof(T) == typeof(string))
            {
                return _cache.SetStringAsync(key, objToAdd?.ToString()!, new DistributedCacheEntryOptions
                {
                    SlidingExpiration = tsExpires
                });
            }

            return _cache.SetStringAsync(key, JsonConvert.SerializeObject(objToAdd), new DistributedCacheEntryOptions
            {
                SlidingExpiration = tsExpires
            });
        }

        public async Task<T> Get<T>(string key) where T : class
        {
            var result = await _cache.GetStringAsync(key);

            if(result == null)
            {
              #pragma warning disable CS8603 // Possible null reference return.
                return null;
              #pragma warning restore CS8603 // Possible null reference return.
            }

            if(typeof(T) == typeof(string))
            {
                var ret = ((object)result) as T;

                return ret!;
            }

            return JsonConvert.DeserializeObject<T>(result)!;
        }

        public Task Remove(string key)
        {
            return _cache.RemoveAsync(key);
        }

        #endregion
    }
}