using System;
using System.Threading.Tasks;

using Willow.Azure.Cache;
using Willow.Common;
using Xunit;

namespace Willow.Azure.Cache.FunctionalTests
{
    public class RedisCacheTests
    {
        private readonly string ConnectionString = "???";

        [Fact(Skip = "Add connection string to run this test")]
        public async Task RedisCacheTests_Get()
        {
            ICache cache = new RedisCache(ConnectionString);

            await cache.Add("husband", "Fred");
            
            var result = await cache.Get<string>("husband");

            Assert.Equal("Fred", result);
        }

        [Fact(Skip = "Add connection string to run this test")]
        public async Task RedisCacheTests_Get_auto()
        {
            ICache cache = new RedisCache(ConnectionString);

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

        [Fact(Skip = "Add connection string to run this test")]
        public async Task RedisCacheTests_Get_absoluteexpiration()
        {
            ICache cache = new RedisCache(ConnectionString);

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

        [Fact(Skip = "Add connection string to run this test")]
        public async Task RedisCacheTests_Get_slidingexpiration()
        {
            ICache cache = new RedisCache(ConnectionString);

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

        [Fact(Skip = "Add connection string to run this test")]
        public async Task RedisCacheTests_Remove()
        {
            ICache cache = new RedisCache(ConnectionString);

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