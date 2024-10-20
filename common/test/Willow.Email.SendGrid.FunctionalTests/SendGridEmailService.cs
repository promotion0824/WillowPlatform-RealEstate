using System.Threading.Tasks;

using Xunit;

using Willow.Email.SendGrid;

namespace Willow.Email.SendGrid
{
    public class SendGridEmailServiceTests
    {
        [Fact(Skip = "Add connection string to run this test")]
        public async Task SendGridEmailService_Send()
        {
            var sendGrid = new SendGridEmailService("???", "no-reply@willowinc.com", "Willow IT");

            await sendGrid.Send("jlightfoot@willowinc.com", "This is a test", "<html><body>This is also a test</body></html>", new { Environment = "jims", Region = "Renton"} );
        }
    }
}