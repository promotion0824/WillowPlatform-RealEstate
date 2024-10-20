using Xunit;
using Moq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Willow.Http.DI;
using Willow.Platform.Common;
using Willow.Communications.Service;
using Willow.PushNotification;

namespace Willow.Communications.Function.FunctionalTests
{
    public class ExtensionsTests
    {
        private const string ClientId          = "tG6FQz91sof74x6KWgYHmrBJr5YSRCVL";
        private const string ClientSecret      = "???";
        private const string ClientPassword    = "???";
        private const string ContentStorageKey = "???";

        [Fact(Skip = "Add secrets above to test")]
        public void AddComvSvc_Success()
        {
            var logger = new Mock<ILogger<Auth0TokenService>>();
            var logger2 = new Mock<ILogger<ServiceCollection>>();

            var config = new Mock<IConfiguration>();

            config.Setup( c=> c["M2MAuthClientId"]).                  Returns(ClientId);
            config.Setup( c=> c["M2MAuthAudience"]).                  Returns("https://willowtwin-web-uat");
            config.Setup( c=> c["M2MAuthClientSecret"]).              Returns(ClientSecret);
            config.Setup( c=> c["M2MAuthUserName"]).                  Returns("functiontesting@function.willowinc.com");
            config.Setup( c=> c["M2MAuthDomain"]).                    Returns("willowtwin-uat.auth0.com");
                                                                      
            config.Setup( c=> c["mkpM2MAuthClientId"]).               Returns(ClientId);
            config.Setup( c=> c["mkpM2MAuthAudience"]).               Returns("https://willowtwin-web-uat");
            config.Setup( c=> c["mkpM2MAuthClientSecret"]).           Returns(ClientSecret);
            config.Setup( c=> c["mkpM2MAuthUserName"]).               Returns("functiontesting@function.willowinc.com");
            config.Setup( c=> c["mkpM2MAuthDomain"]).                 Returns("willowtwin-uat.auth0.com");
                                                                      
            config.Setup( c=> c["DirectoryCoreBaseAddress"]).         Returns("https://wil-uat-plt-eu21-directorycore.azurewebsites.net");
            config.Setup( c=> c["PushNotificationConnectionString"]). Returns("blah!");
            config.Setup( c=> c["PushNotificationHubPath"]).          Returns("blah!");

            config.Setup( c=> c["SendGridApiKey"]).                   Returns("blah!");
            config.Setup( c=> c["EmailFromAddress"]).                 Returns("blah!");
            config.Setup( c=> c["EmailFromName"]).                    Returns("blah!");
            
            config.Setup( c=> c["ContentStorageAccountName"]).        Returns("wiluatplteu21contentsto");
            config.Setup( c=> c["ContentStorageContainerName"]).      Returns("realestate");
            config.Setup( c=> c["ContentStorageAccountKey"]).         Returns(ContentStorageKey);

            
            var services = new ServiceCollection();
           
            services.AddSingleton(logger.Object);
            services.AddSingleton(logger2.Object);
            services.AddSingleton(config.Object);
            
            services.AddCommSvc();
            
            services.AddSingleton( p=> new Mock<IPushNotificationService>().Object);

            var sp = services.BuildServiceProvider(new ServiceProviderOptions()
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

            Assert.NotNull(sp.GetService<IRecipientResolver>());
            Assert.NotNull(sp.GetService<ICommunicationsService>());
        }
    }
}