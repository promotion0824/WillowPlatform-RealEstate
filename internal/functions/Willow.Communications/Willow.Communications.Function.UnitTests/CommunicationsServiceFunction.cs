using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Xunit;
using Moq;

using Willow.Common;
using Willow.Data;
using Willow.Communications.Service;
using Willow.Platform.Common;
using Willow.Platform.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Willow.Platform.Mocks;
using Willow.Functions.Common;

namespace Willow.Communications.Function.UnitTests
{
    public class CommunicationsServiceUnitTests
    {
        private readonly CommunicationsServiceFunction _function;
        private readonly ICommunicationsService _svc;
        private readonly FakeLogger _logger;
        private readonly Mock<FunctionContext> _context;
        private readonly Mock<IBlobStore> _templateStore;
        private readonly Mock<IRecipientResolver> _recipientResolver;
        private readonly Mock<INotificationHub> _notificationHub = new();
        private readonly Mock<IReadRepository<Guid, Customer>> _customerRepo = new();
        private readonly Guid _customerId = Guid.NewGuid();
        private readonly Guid _userId = Guid.NewGuid();
        private readonly UserType? _userType = UserType.Customer;

        public CommunicationsServiceUnitTests()
        {
            var hubs = new Dictionary<string, INotificationHub>
            {
              {"email", _notificationHub.Object},
              {"pushnotification", _notificationHub.Object}
            };

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<ILoggerFactory, FakeLoggerFactory>();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            _context = new Mock<FunctionContext>();
            _context.SetupProperty(c => c.InstanceServices, serviceProvider);

            _logger = (serviceProvider.GetRequiredService<ILoggerFactory>() as FakeLoggerFactory).GetFakeLogger();

            _templateStore = new Mock<IBlobStore>();
            _recipientResolver = new Mock<IRecipientResolver>();

            _customerRepo.Setup(r => r.Get(_customerId)).ReturnsAsync(new Customer { Id = _customerId, Name = "Bedrock", Status = Customer.CustomerStatus.Active });

            _recipientResolver.Setup(r => r.GetRecipient(_customerId, _userId, _userType, "pushnotification", It.IsAny<IDictionary<string, object>>())).ReturnsAsync((_userId.ToString(), "en"))
                                           .Callback((Guid customerId, Guid userId, UserType? userType, string notificationType, IDictionary<string, object> data) => data.Add("UserName", "Fred Flintstone"));

            _templateStore.Setup(s => s.Get("email/en/test.html", It.IsAny<Stream>())).Callback<string, Stream>(async (id, dest) =>
            {
                await dest.WriteStringAsync("<html><title>Welcome {UserName}</title><body>Dear {UserName}, You are now a user, {NickName}.</body></html>");
            });

            _templateStore.Setup(s => s.Get("email/en/AssignSuperUserRole.html", It.IsAny<Stream>())).Callback<string, Stream>(async (id, dest) =>
            {
                await dest.WriteStringAsync("<html><title>Welcome {UserName}</title><body>Dear {UserName}, You are now a user, {NickName}.</body></html>");
            });

            _templateStore.Setup(s => s.Get("email/fr/test.html", It.IsAny<Stream>())).Callback<string, Stream>(async (id, dest) =>
            {
                await dest.WriteStringAsync("<html><title>Bienvenue {UserName}</title><body>Cher {UserName}, Tu es maintenant un utilisateur, {NickName}.</body></html>");
            });

            _templateStore.Setup(s => s.Get("pushnotification/en/test.xml", It.IsAny<Stream>())).Callback<string, Stream>(async (id, dest) =>
            {
                await dest.WriteStringAsync("<xml><subject>Welcome {UserName}</subject><body>Dear {UserName}, You are now a user, {NickName}.</body></xml>");
            });

            _svc = new CommunicationsService(hubs, _templateStore.Object, _recipientResolver.Object, _customerRepo.Object, _logger);
            _function = new CommunicationsServiceFunction(_svc);
        }

        [Theory]
        [InlineData("customeruser", "en", "en", "en", "Welcome Fred Flintstone", "<html><title>Welcome Fred Flintstone</title><body>Dear Fred Flintstone, You are now a user, dude.</body></html>")]
        [InlineData("customeruser", "es", "fr", "fr", "Bienvenue Fred Flintstone", "<html><title>Bienvenue Fred Flintstone</title><body>Cher Fred Flintstone, Tu es maintenant un utilisateur, dude.</body></html>")]
        [InlineData("customeruser", null, "fr", "fr", "Bienvenue Fred Flintstone", "<html><title>Bienvenue Fred Flintstone</title><body>Cher Fred Flintstone, Tu es maintenant un utilisateur, dude.</body></html>")]
        [InlineData("customeruser", "fr", null, "fr", "Bienvenue Fred Flintstone", "<html><title>Bienvenue Fred Flintstone</title><body>Cher Fred Flintstone, Tu es maintenant un utilisateur, dude.</body></html>")]
        [InlineData(null, "fr", null, "fr", "Bienvenue Fred Flintstone", "<html><title>Bienvenue Fred Flintstone</title><body>Cher Fred Flintstone, Tu es maintenant un utilisateur, dude.</body></html>")]
        [InlineData("Customer", "fr", null, "fr", "Bienvenue Fred Flintstone", "<html><title>Bienvenue Fred Flintstone</title><body>Cher Fred Flintstone, Tu es maintenant un utilisateur, dude.</body></html>")]
        public async Task CommunicationsServiceFunction_Run(string userType, string userLanguage, string locale, string languageUsed, string subject, string body)
        {
            _recipientResolver.Setup(r => r.GetRecipient(_customerId, _userId, userType.ToUserType(), "email", It.IsAny<IDictionary<string, object>>()))
                                           .ReturnsAsync(("fred.flintstone@bedrock.com", userLanguage))
                                           .Callback((Guid customerId, Guid userId, UserType? userType, string notificationType, IDictionary<string, object> data) => data.Add("UserName", "Fred Flintstone"));

            await _function.Run(JsonConvert.SerializeObject(new NotificationMessage
            {
                CorrelationId = Guid.NewGuid(),
                CustomerId = _customerId,
                UserId = _userId,
                UserType = userType,
                Locale = locale,
                CommunicationType = "email",
                TemplateName = "test",
                Data = new Dictionary<string, string> { { "NickName", "dude" } },
                Tags = new Dictionary<string, string> { { "Environment", "dev" }, { "Region", "aue1" } }
            }), _context.Object);

            Assert.Equal("Sending email", _logger.Entries[1].Message);
            Assert.Equal("CommunicationsService", _logger.Entries[0].Properties["FunctionName"]);
            Assert.Equal(_customerId, _logger.Entries[1].Properties["CustomerId"]);
            Assert.Equal(_userId, _logger.Entries[1].Properties["UserId"]);
            Assert.Equal(languageUsed, _logger.Entries[1].Properties["Locale"]);
            Assert.Equal("test", _logger.Entries[1].Properties["TemplateName"]);
            Assert.Equal("Bedrock", _logger.Entries[1].Properties["CustomerName"]);

            _notificationHub.Verify(s => s.Send("fred.flintstone@bedrock.com",
                                               subject,
                                               body,
                                               new Dictionary<string, object> { { "Environment", "dev" }, { "Region", "aue1" } }), Times.Once);
        }

        [Fact]
        public async Task CommunicationsServiceFunction_Run_deserialize()
        {
            var customerId = Guid.Parse("59ee8082-f750-4a92-b4e2-2d760d8341bf");
            var userId = Guid.Parse("AF3F1F3D-923E-473B-AD92-B6B825EF2BB5");

            _customerRepo.Setup(r => r.Get(customerId)).ReturnsAsync(new Customer { Id = customerId, Name = "Bedrock", Status = Customer.CustomerStatus.Active });

            _recipientResolver.Setup(r => r.GetRecipient(customerId,
                                                         userId,
                                                         UserType.Customer,
                                                         "email",
                                                         It.IsAny<IDictionary<string, object>>()))
                                           .ReturnsAsync(("fred.flintstone@bedrock.com", "en"))
                                           .Callback((Guid customerId, Guid userId, UserType? userType, string notificationType, IDictionary<string, object> data) => data.Add("UserName", "Fred Flintstone"));

            await _function.Run(_testMsg, _context.Object);

            Assert.Equal("Sending email", _logger.Entries[1].Message);
            Assert.Equal("CommunicationsService", _logger.Entries[0].Properties["FunctionName"]);
            Assert.Equal(customerId, _logger.Entries[1].Properties["CustomerId"]);
            Assert.Equal(userId, _logger.Entries[1].Properties["UserId"]);
            Assert.Equal("en", _logger.Entries[1].Properties["Locale"]);
            Assert.Equal("AssignSuperUserRole", _logger.Entries[1].Properties["TemplateName"]);
            Assert.Equal("Bedrock", _logger.Entries[1].Properties["CustomerName"]);

            _notificationHub.Verify(s => s.Send("fred.flintstone@bedrock.com",
                                               "Welcome Fred Flintstone",
                                               "<html><title>Welcome Fred Flintstone</title><body>Dear Fred Flintstone, You are now a user, dude.</body></html>",
                                               new Dictionary<string, object> { { "Environment", "dev" }, { "Region", "aue1" } }), Times.Once);
        }

        [Fact]
        public async Task CommunicationsServiceFunction_PushNotification_Run()
        {
            await _function.Run(JsonConvert.SerializeObject(new NotificationMessage
            {
                CorrelationId = Guid.NewGuid(),
                CustomerId = _customerId,
                UserId = _userId,
                UserType = "customeruser",
                Locale = "en",
                CommunicationType = "pushnotification",
                TemplateName = "test",
                Data = new Dictionary<string, string> { { "NickName", "dude" } },
                Tags = new Dictionary<string, string> { { "Environment", "dev" }, { "Region", "aue1" } }
            }), _context.Object);

            Assert.Equal("Sending pushnotification", _logger.Entries[1].Message);
            Assert.Equal("CommunicationsService", _logger.Entries[0].Properties["FunctionName"]);
            Assert.Equal(_customerId, _logger.Entries[1].Properties["CustomerId"]);
            Assert.Equal(_userId, _logger.Entries[1].Properties["UserId"]);
            Assert.Equal("en", _logger.Entries[1].Properties["Locale"]);
            Assert.Equal("test", _logger.Entries[1].Properties["TemplateName"]);
            Assert.Equal("Bedrock", _logger.Entries[1].Properties["CustomerName"]);

            _notificationHub.Verify(s => s.Send(_userId.ToString(),
                                                "Welcome Fred Flintstone", "Dear Fred Flintstone, You are now a user, dude.",
                                                new Dictionary<string, object> { { "Environment", "dev" }, { "Region", "aue1" } }), Times.Once);
        }

        [Fact]
        public async Task CommunicationsServiceFunction_PushNotificationNoUserType_Run()
        {
            await _function.Run(JsonConvert.SerializeObject(new NotificationMessage
            {
                CorrelationId = Guid.NewGuid(),
                CustomerId = _customerId,
                UserId = _userId,
                UserType = null,
                Locale = "en",
                CommunicationType = "pushnotification",
                TemplateName = "test",
                Data = new Dictionary<string, string> { { "NickName", "dude" } },
                Tags = new Dictionary<string, string> { { "Environment", "dev" }, { "Region", "aue1" } }
            }), _context.Object);

            _notificationHub.Verify(s => s.Send(_userId.ToString(),
                                                "Welcome Fred Flintstone", "Dear Fred Flintstone, You are now a user, dude.",
                                                new Dictionary<string, object> { { "Environment", "dev" }, { "Region", "aue1" } }), Times.Once);
        }

        [Theory]
        [InlineData("customeruser123")]
        [InlineData("Workgroup")]
        public async Task CommunicationsServiceFunction_PushNotification_Run_Validation_Error(string userType)
        {
            var exception = await Assert.ThrowsAsync<InvalidMessageException>(() => _function.Run(JsonConvert.SerializeObject(new NotificationMessage
            {
                CorrelationId = Guid.NewGuid(),
                CustomerId = _customerId,
                UserId = _userId,
                UserType = userType,
                Locale = "en#@!",
                CommunicationType = "pushnotification1",
                TemplateName = "test@23847jsh$%",
                Data = new Dictionary<string, string> { { "NickName", "dude" } },
                Tags = new Dictionary<string, string> { { "Environment", "dev" }, { "Region", "aue1" } }
            }), _context.Object));

            Assert.Equal(4, exception.Errors.Count());

            Assert.Equal("Validation failed for NotificationMessage", _logger.Entries[0].Message);
            Assert.Equal("Please enter one of the allowable values: email, pushnotification.", _logger.Entries[2].Message);
            Assert.Equal("Locale has non alphanumeric characters in it", _logger.Entries[3].Message);
            Assert.Equal("TemplateName has non alphanumeric characters in it", _logger.Entries[4].Message);

            _notificationHub.Verify(s => s.Send(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        }

        private static string _testMsg =
        @"{
            'CorrelationId':        '10235C53-DEB5-4220-9F94-FA0838E4230A',
            'CustomerId':           '59ee8082-f750-4a92-b4e2-2d760d8341bf', // uat-us
            'UserId':               'AF3F1F3D-923E-473B-AD92-B6B825EF2BB5',
            'UserType':             'Customer',
            'CommunicationType':    'email',
            'Locale':               'en',
            'TemplateName':         'AssignSuperUserRole',
            'Data': 
            {
                'LoginUrl':         'https://no.where.com',
                'NickName':         'dude'
            },
            'Tags': 
            {
                'Environment':      'dev',
                'Region':           'aue1'
            }
        }";
    }
}