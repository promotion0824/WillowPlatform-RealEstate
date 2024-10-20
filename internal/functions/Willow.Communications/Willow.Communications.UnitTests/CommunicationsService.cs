using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Xunit;
using Moq;

using Willow.Common;
using Willow.Data;
using Willow.Platform.Common;
using Willow.Platform.Models;
using Willow.Communications.Service;

namespace Willow.Communications.UnitTests
{
    public class CommunicationsServiceTests
    {
        private readonly Mock<INotificationHub> _notificationHub;
        private readonly Mock<IBlobStore> _templateStore;
        private readonly Mock<IRecipientResolver> _recipientResolver;
        private readonly Mock<IReadRepository<Guid, Customer>> _customerRepo = new Mock<IReadRepository<Guid, Customer>>();
        private readonly Guid _customerId = Guid.NewGuid();
        private readonly Guid _userId = Guid.NewGuid();
        private readonly UserType? _userType = UserType.Customer;
        private readonly ICommunicationsService _svc;

        public CommunicationsServiceTests()
        {
            _notificationHub = new Mock<INotificationHub>();
            _templateStore = new Mock<IBlobStore>();
            _recipientResolver = new Mock<IRecipientResolver>();
            var logger = new Mock<ILogger>();

            _recipientResolver.Setup( r=> r.GetRecipient(_customerId, _userId, _userType, "email", It.IsAny<IDictionary<string, object>>())).ReturnsAsync(("bob.jones@nowhere.com", "en"));
            _recipientResolver.Setup( r=> r.GetRecipient(_customerId, _userId, _userType, "pushnotification", It.IsAny<IDictionary<string, object>>())).ReturnsAsync((_userId.ToString(), "en"));

            _svc = new CommunicationsService(new Dictionary<string, INotificationHub> 
            { 
                { "email", _notificationHub.Object },
                { "pushnotification", _notificationHub.Object },
            }, _templateStore.Object, _recipientResolver.Object, _customerRepo.Object, logger.Object);
        }

        [Fact]
        public async Task CommunicationsServiceTests_SendNotification_fails()
        {
            await Assert.ThrowsAsync<ArgumentException>( async ()=> await _svc.SendNotification(_customerId, _userId, _userType, "bob", "en", new { }, null, "bob"));
        }        
        
        [Fact]
        public async Task CommunicationsServiceTests_SendNotification_succeeds()
        {
            _templateStore.Setup( s=> s.Get("email/en/bob.html", It.IsAny<Stream>())).Callback<string, Stream>( async (id, dest)=> 
            {
                await dest.WriteAsync("<html><title>Welcome {UserName}</title><body>Dear {UserName}, You are now a user.</body></html>");
            });

            await _svc.SendNotification(_customerId, _userId, _userType, "bob", "en", new { UserName = "Fred Flintstone"}, new { Environment = "dev", Region = "aue1" }, "email");

            _notificationHub.Verify( s=> s.Send("bob.jones@nowhere.com", 
                                                "Welcome Fred Flintstone", 
                                                "<html><title>Welcome Fred Flintstone</title><body>Dear Fred Flintstone, You are now a user.</body></html>", 
                                                new Dictionary<string, object> { {"Environment", "dev"}, {"Region", "aue1"} } ), Times.Once);               
        }

        [Fact]
        public async Task CommunicationsServiceTests_SendNotification_russian_succeeds()
        {
            _templateStore.Setup( s=> s.Get("email/en/bob.html", It.IsAny<Stream>())).Callback<string, Stream>( async (id, dest)=> 
            {
                await dest.WriteAsync("<html><title>Welcome {UserName}</title><body>Dear {UserName}, You are now a user.</body></html>");
            });

            _templateStore.Setup( s=> s.Get("email/ru/bob.html", It.IsAny<Stream>())).Callback<string, Stream>( async (id, dest)=> 
            {
                await dest.WriteAsync("<html><title>Добро пожаловать {UserName}</title><body>Уважаемый {UserName}, Теперь Вы пользователь.</body></html>");
            });

            await _svc.SendNotification(_customerId, _userId, _userType, "bob", "ru", new { UserName = "Дмитрий"}, null, "email");

            _notificationHub.Verify( s=> s.Send("bob.jones@nowhere.com", "Добро пожаловать Дмитрий", "<html><title>Добро пожаловать Дмитрий</title><body>Уважаемый Дмитрий, Теперь Вы пользователь.</body></html>", It.IsAny<object>()), Times.Once);               
        }

        [Fact]
        public async Task CommunicationsServiceTests_SendNotification_russian_fall_back()
        {
            _templateStore.Setup( s=> s.Get("email/en/bob.html", It.IsAny<Stream>())).Callback<string, Stream>( async (id, dest)=> 
            {
                await dest.WriteAsync("<html><title>Welcome {UserName}</title><body>Dear {UserName}, You are now a user.</body></html>");
            });

            await _svc.SendNotification(_customerId, _userId, _userType, "bob", "en", new { UserName = "Дмитрий"}, null, "email");

            _notificationHub.Verify( s=> s.Send("bob.jones@nowhere.com", "Welcome Дмитрий", "<html><title>Welcome Дмитрий</title><body>Dear Дмитрий, You are now a user.</body></html>", It.IsAny<object>()), Times.Once);               
        }

        [Fact]
        public async Task CommunicationsServiceTests_SendPushNotification_succeeds()
        {
            _templateStore.Setup(s => s.Get("pushnotification/en/bob.xml", It.IsAny<Stream>())).Callback<string, Stream>(async (id, dest) =>
            {
                await dest.WriteAsync("<xml><subject>Welcome {UserName}</subject><body>Dear {UserName}, You are now a user.</body></xml>");
            });

            await _svc.SendNotification(_customerId, _userId, _userType, "bob", "en", new { UserName = "Fred Flintstone" }, new { Environment = "dev", Region = "aue1" }, "pushnotification");

            _notificationHub.Verify(s => s.Send(_userId.ToString(),
                                                "Welcome Fred Flintstone", 
                                                "Dear Fred Flintstone, You are now a user.", 
                                                new Dictionary<string, object> { {"Environment", "dev"}, {"Region", "aue1"} }), Times.Once);
        }

        [Fact]
        public async Task CommunicationsServiceTests_SendPushNotification_russian_succeeds()
        {
            _templateStore.Setup(s => s.Get("pushnotification/en/bob.xml", It.IsAny<Stream>())).Callback<string, Stream>(async (id, dest) =>
            {
                await dest.WriteAsync("<xml><subject>Welcome {UserName}</subject><body>Dear {UserName}, You are now a user.</body></xml>");
            });

            _templateStore.Setup(s => s.Get("pushnotification/ru/bob.xml", It.IsAny<Stream>())).Callback<string, Stream>(async (id, dest) =>
            {
                await dest.WriteAsync("<xml><subject>Добро пожаловать {UserName}</subject><body>Уважаемый {UserName}, Теперь Вы пользователь.</body></xml>");
            });

            await _svc.SendNotification(_customerId, _userId, _userType, "bob", "ru", new { UserName = "Дмитрий" }, It.IsAny<object>(), "pushnotification");

            _notificationHub.Verify(s => s.Send(_userId.ToString(), "Добро пожаловать Дмитрий", "Уважаемый Дмитрий, Теперь Вы пользователь.", It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task CommunicationsServiceTests_SendPushNotification_russian_fall_back()
        {
            _templateStore.Setup(s => s.Get("pushnotification/en/bob.xml", It.IsAny<Stream>())).Callback<string, Stream>(async (id, dest) =>
            {
                await dest.WriteAsync("<xml><subject>Welcome {UserName}</subject><body>Dear {UserName}, You are now a user.</body></xml>");
            });

            await _svc.SendNotification(_customerId, _userId, _userType, "bob", "en", new { UserName = "Дмитрий" }, null, "pushnotification");

            _notificationHub.Verify(s => s.Send(_userId.ToString(), "Welcome Дмитрий", "Dear Дмитрий, You are now a user.", It.IsAny<object>()), Times.Once);
        }
    }

    public static class Extensions
    {
        public static Task WriteAsync(this Stream stream, string data, Encoding encoder = null)
        {
            encoder = encoder ?? UTF8Encoding.UTF8;

            byte[] bytes = encoder.GetBytes(data);
       
            return stream.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}
