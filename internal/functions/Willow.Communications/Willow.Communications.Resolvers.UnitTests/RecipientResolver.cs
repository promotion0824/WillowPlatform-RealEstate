using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Xunit;
using Moq;

using Willow.Data;
using Willow.Platform.Models;

namespace Willow.Communications.Resolvers.UnitTests
{
    public class RecipientResolverTests
    {
        [Theory]
        [InlineData(UserStatus.Active, "email", "bob")]
        [InlineData(UserStatus.Pending, "email", "bob")]
        [InlineData(UserStatus.Active, "pushnotification", "7afbe2a6-5861-48fb-b111-0db8a32552b5")]
        [InlineData(UserStatus.Pending, "pushnotification", "7afbe2a6-5861-48fb-b111-0db8a32552b5")]
        public async Task RecipientResolver_GetRecipient(UserStatus status, string notificationType, string result)
        {
            var userRepo = new Mock<IReadRepository<UserRequest, User>>();

            var resolver = new RecipientResolver(userRepo.Object);
            var data = new Dictionary<string, object>();

            userRepo.Setup( r=> r.Get(It.IsAny<UserRequest>())).ReturnsAsync(new User { Id = Guid.Parse("7afbe2a6-5861-48fb-b111-0db8a32552b5"), Email = "bob", Mobile = "1234", Status = status, FirstName = "Bob", LastName = "Jones" });

            Assert.Equal(result,  (await resolver.GetRecipient(Guid.NewGuid(), Guid.Parse("7afbe2a6-5861-48fb-b111-0db8a32552b5"), UserType.Customer, notificationType, data)).Address);
            Assert.Equal("Bob",   data["FirstName"]);
            Assert.Equal("Jones", data["LastName"]);
        }

        [Theory]
        [InlineData(UserStatus.Inactive, "email")]
        [InlineData(UserStatus.Deleted, "email")]
        public async Task RecipientResolver_GetRecipient_throws_Exception(UserStatus status, string notificationType)
        {
            var userRepo = new Mock<IReadRepository<UserRequest, User>>();
            var resolver = new RecipientResolver(userRepo.Object);
            var data = new Dictionary<string, object>();

            userRepo.Setup( r=> r.Get(It.IsAny<UserRequest>())).ReturnsAsync(new User { Email = "bob", Mobile = "1234",Status = status, FirstName = "Bob", LastName = "Jones" });

            await Assert.ThrowsAsync<InvalidUserStatusException>( async ()=> await resolver.GetRecipient(Guid.NewGuid(), Guid.NewGuid(), UserType.Customer, notificationType, data));
        }

        [Theory]
        [InlineData(UserStatus.Active, "sms")]
        [InlineData(UserStatus.Pending, "sms")]
        public async Task RecipientResolver_GetRecipient_throws_ArgumentException(UserStatus status, string notificationType)
        {
            var userRepo = new Mock<IReadRepository<UserRequest, User>>();

            var resolver = new RecipientResolver(userRepo.Object);
            var data = new Dictionary<string, object>();

            userRepo.Setup( r=> r.Get(It.IsAny<UserRequest>())).ReturnsAsync(new User { Email = "bob", Mobile = "1234",Status = status, FirstName = "Bob", LastName = "Jones" });

            await Assert.ThrowsAsync<ArgumentException>( async ()=> await resolver.GetRecipient(Guid.NewGuid(), Guid.NewGuid(), UserType.Customer, notificationType, data));
        }
    }
}