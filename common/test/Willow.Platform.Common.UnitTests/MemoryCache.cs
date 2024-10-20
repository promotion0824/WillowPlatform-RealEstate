using System;
using System.Threading.Tasks;

using MSCache = Microsoft.Extensions.Caching.Memory;
using Xunit;

using Willow.Common;

namespace Willow.Platform.Common
{
    public class MemoryCacheTests
    {
        [Fact]
        public async Task MemoryCache_Get()
        {
            ICache cache = new MemoryCache(new MSCache.MemoryCache(new MSCache.MemoryCacheOptions { }));

            await cache.Add("husband", "Fred");
            
            var result = await cache.Get<string>("husband");

            Assert.Equal("Fred", result);
        }

        [Fact]
        public async Task MemoryCache_Get_auto()
        {
            ICache cache = new MemoryCache(new MSCache.MemoryCache(new MSCache.MemoryCacheOptions { }));

            await cache.Add("mycar", new Automobile
            {
                Make          = "Chevy",
                Model         = "Camaro",
                Color         = "Blue",
                Year          = 1969,
                Cylinders     = 8,
                Displacement  = 350
            });
            
            var result = await cache.Get<Automobile>("mycar");

            Assert.NotNull(result);
            Assert.Equal("Chevy",  result.Make);
            Assert.Equal("Camaro", result.Model);
            Assert.Equal("Blue",   result.Color);
            Assert.Equal(1969,     result.Year);
        }

        [Fact]
        public async Task MemoryCache_Get_absoluteexpiration()
        {
            ICache cache = new MemoryCache(new MSCache.MemoryCache(new MSCache.MemoryCacheOptions { }));

            await cache.Add("mycar", new Automobile
            {
                Make          = "Chevy",
                Model         = "Camaro",
                Color         = "Blue",
                Year          = 1969,
                Cylinders     = 8,
                Displacement  = 350
            },
            DateTime.UtcNow.AddMinutes(10));
            
            var result = await cache.Get<Automobile>("mycar");

            Assert.NotNull(result);
            Assert.Equal("Chevy",  result.Make);
            Assert.Equal("Camaro", result.Model);
            Assert.Equal("Blue",   result.Color);
            Assert.Equal(1969,     result.Year);
        }

        [Fact]
        public async Task MemoryCache_Get_absoluteexpiration_expired()
        {
            ICache cache = new MemoryCache(new MSCache.MemoryCache(new MSCache.MemoryCacheOptions { }));

            await cache.Add("mycar", new Automobile
            {
                Make          = "Chevy",
                Model         = "Camaro",
                Color         = "Blue",
                Year          = 1969,
                Cylinders     = 8,
                Displacement  = 350
            },
            DateTime.UtcNow.AddSeconds(3));
            
            var result = await cache.Get<Automobile>("mycar");

            Assert.NotNull(result);
            Assert.Equal("Chevy",  result.Make);
            Assert.Equal("Camaro", result.Model);
            Assert.Equal("Blue",   result.Color);
            Assert.Equal(1969,     result.Year);
            
            // Wait for the cache to expire
            await Task.Delay(3500);

            var result2 = await cache.Get<Automobile>("mycar");

            Assert.Null(result2);
        }

        [Fact]
        public async Task MemoryCache_Get_slidingexpiration()
        {
            ICache cache = new MemoryCache(new MSCache.MemoryCache(new MSCache.MemoryCacheOptions { }));

            await cache.Add("mycar", new Automobile
            {
                Make          = "Chevy",
                Model         = "Camaro",
                Color         = "Blue",
                Year          = 1969,
                Cylinders     = 8,
                Displacement  = 350
            },
            TimeSpan.FromMinutes(10));
            
            var result = await cache.Get<Automobile>("mycar");

            Assert.NotNull(result);
            Assert.Equal("Chevy",  result.Make);
            Assert.Equal("Camaro", result.Model);
            Assert.Equal("Blue",   result.Color);
            Assert.Equal(1969,     result.Year);
        }

        [Fact]
        public async Task MemoryCache_Get_slidingexpiration_expired()
        {
            ICache cache = new MemoryCache(new MSCache.MemoryCache(new MSCache.MemoryCacheOptions { }));

            await cache.Add("mycar", new Automobile
            {
                Make          = "Chevy",
                Model         = "Camaro",
                Color         = "Blue",
                Year          = 1969,
                Cylinders     = 8,
                Displacement  = 350
            },
            TimeSpan.FromSeconds(3));
            
            var result = await cache.Get<Automobile>("mycar");

            Assert.NotNull(result);
            Assert.Equal("Chevy",  result.Make);
            Assert.Equal("Camaro", result.Model);
            Assert.Equal("Blue",   result.Color);
            Assert.Equal(1969,     result.Year);
            
            // Wait for the cache to expire
            await Task.Delay(3500);

            var result2 = await cache.Get<Automobile>("mycar");

            Assert.Null(result2);
        }

        [Fact]
        public async Task MemoryCache_Remove()
        {
            ICache cache = new MemoryCache(new MSCache.MemoryCache(new MSCache.MemoryCacheOptions { }));

            await cache.Add("husband", "Fred");
            
            var result = await cache.Get<string>("husband");

            Assert.Equal("Fred", result);
            
            await cache.Remove("husband");

            result = await cache.Get<string>("husband");

            Assert.Null(result);
        }

        public class Automobile
        {
            public string Make          { get; set; } = "";
            public string Model         { get; set; } = "";
            public string Color         { get; set; } = "";
            public int    Year          { get; set; }
            public int    Cylinders     { get; set; }
            public int    Displacement  { get; set; }
        }
    }
}