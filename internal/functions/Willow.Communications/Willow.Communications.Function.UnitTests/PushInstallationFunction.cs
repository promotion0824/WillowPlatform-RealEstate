using System;
using System.Threading.Tasks;

using Xunit;
using Moq;

using Willow.PushNotification;
using AutoFixture;
using Newtonsoft.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Willow.Platform.Mocks;

namespace Willow.Communications.Function.UnitTests
{
    public class PushInstallationFunctionUnitTests
    {
        private readonly PushInstallationFunction _function;
        private readonly Mock<IPushNotificationService> _svc;
        private readonly Mock<FunctionContext> _context;
        private readonly FakeLogger _logger; 
        private readonly Guid _userId = Guid.NewGuid();

        public PushInstallationFunctionUnitTests()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<ILoggerFactory, FakeLoggerFactory>();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            _context = new Mock<FunctionContext>();
            _context.SetupProperty(c => c.InstanceServices, serviceProvider);

            _logger = (serviceProvider.GetRequiredService<ILoggerFactory>() as FakeLoggerFactory).GetFakeLogger();

            _svc           = new Mock<IPushNotificationService>();
            _function      = new PushInstallationFunction(_svc.Object);
        }

        [Fact]
        public async Task CommunicationsServiceFunction_Run()
        {
            var message = new Fixture().Build<PushInstallationMessage>()
                                       .With(x => x.UserId, _userId)
                                       .With(x => x.Action, "delete")
                                       .Create();

            await _function.Run(JsonConvert.SerializeObject(message), _context.Object);

            Assert.Equal("PushInstallation", _logger.Entries[0].Properties["FunctionName"]);
            Assert.Equal(_userId, _logger.Entries[1].Properties["UserId"]);
            Assert.Equal("delete", _logger.Entries[1].Properties["Action"]);
        }
    }
}