using System;
using System.Net.Http;
using System.Threading.Tasks;

using Xunit;
using Moq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Willow.Api.Client;

namespace Willow.Http.DI.FunctionalTests
{
    public class ExtensionsTests
    {
        private const string ClientId       = "tG6FQz91sof74x6KWgYHmrBJr5YSRCVL";
        private const string ClientSecret   = "???";
        private const string ClientPassword = "???";

        [Theory(Skip = "Add credentials to run this test")]
        [InlineData("",    ClientPassword)]
        [InlineData("",    null)]
        [InlineData("mkp", ClientPassword)]
        [InlineData("mkp", null)]
        public async Task SetHttpClient_Success(string m2mName, string? password)
        {
            var logger = new Mock<ILogger<Auth0TokenService>>();
            var logger2 = new Mock<ILogger<ServiceCollection>>();

            var config = new Mock<IConfiguration>();

            config.Setup( c=> c["M2MAuthClientId"]).          Returns(ClientId);
            config.Setup( c=> c["M2MAuthAudience"]).          Returns("https://willowtwin-web-uat");
            config.Setup( c=> c["M2MAuthClientSecret"]).      Returns(ClientSecret);
            config.Setup( c=> c["M2MAuthUserName"]).          Returns("functiontesting@function.willowinc.com");
            config.Setup( c=> c["M2MAuthPassword"]).          Returns(password);
            config.Setup( c=> c["M2MAuthDomain"]).            Returns("willowtwin-uat.auth0.com");

            config.Setup( c=> c["mkpM2MAuthClientId"]).       Returns(ClientId);
            config.Setup( c=> c["mkpM2MAuthAudience"]).       Returns("https://willowtwin-web-uat");
            config.Setup( c=> c["mkpM2MAuthClientSecret"]).   Returns(ClientSecret);
            config.Setup( c=> c["mkpM2MAuthUserName"]).       Returns("functiontesting@function.willowinc.com");
            config.Setup( c=> c["mkpM2MAuthPassword"]).       Returns(password);
            config.Setup( c=> c["mkpM2MAuthDomain"]).         Returns("willowtwin-uat.auth0.com");

            config.Setup( c=> c["DirectoryCoreBaseAddress"]). Returns("https://wil-uat-plt-eu21-directorycore.azurewebsites.net");

            var services = new ServiceCollection();

            services.AddMemoryCache();
            services.AddSingleton(logger.Object);
            services.AddSingleton(logger2.Object);
            services.AddSingleton(config.Object);
            services.AddHttpClient();
            services.AddAuth0(new[] { "mkp"} );
            services.SetHttpClient<Auth0TokenService>("DirectoryCore", m2mName, false);

            var sp = services.BuildServiceProvider(new ServiceProviderOptions()
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

            var api = new RestApi( sp.GetRequiredService<IHttpClientFactory>(), "DirectoryCore");
            var response = await api.Get<SiteDto>("sites/4e5fc229-ffd9-462a-882b-16b4a63b2a8a"); // 1MW in uat

            Assert.NotNull(response);
            Assert.Equal("One Manhattan West", response.Name);
        }
    }

    public class SiteDto
    {
        public Guid    Id           { get; set; }
        public Guid    CustomerId   { get; set; }
        public string? Name         { get; set; }
        public string? Code         { get; set; }
        public string? TimezoneId   { get; set; }
        public string? WebMapId     { get; set; }
    }
}