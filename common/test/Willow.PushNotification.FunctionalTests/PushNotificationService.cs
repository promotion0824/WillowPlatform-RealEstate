using System.Threading.Tasks;
using Willow.PushNotification;
using Xunit;


namespace Willow.Email.SendGrid
{
    public class SendGridEmailServiceTests
    {
        [Fact(Skip = "Add connection string to run this test")]
        //[Fact]
        public async Task PushNotificationService_Send()
        {
            var pushNotify = new PushNotificationService(
                "???",
                "wil-uat-plt-eu21-nhb");

            await pushNotify.Send("aaef6285-16d0-4f85-a915-75f7e31d6984", "This is a test", "This is also a test", null);
        }
    }
}