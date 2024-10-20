using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Willow.CommunicationService.Deployment.Tests
{
    public class TriggerEmailsTests : IDisposable
    {
        private readonly string connectionString;
        private readonly ServiceBusClient client;
        private readonly ServiceBusSender sender;
        private readonly Settings settings;

        private readonly IConfiguration _configuration;

        public TriggerEmailsTests()
        {
            // get config
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            settings = _configuration.GetRequiredSection("Settings").Get<Settings>();
            connectionString = settings.ServiceBusConnectionString;
           

            client = new ServiceBusClient(connectionString);
            sender = client.CreateSender("commsvc");
        }

        [Theory]
        [InlineData("AssignSuperUserRole")]
        [InlineData("ContactRequest")]
        [InlineData("InitializeUser")]
        [InlineData("InspectionSummary")]
        [InlineData("PortfolioAssigned")]
        [InlineData("ResetPassword")]
        [InlineData("SiteAssigned")]
        [InlineData("TicketAssigned")]
        [InlineData("TicketCreated")]
        [InlineData("TicketReassigned")]
        [InlineData("TicketResolved")]
        [InlineData("TicketUpdated")]
        public async Task Trigger_emails_in_english(string templateFile)
        {
            var fileName = templateFile;
           
            // message payload
            var template = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Templates", String.Concat(fileName, ".json")));

            // inject values from config
            JObject templateObj = JObject.Parse(template);
            templateObj["CorrelationId"] = settings.CorrelationId;
            templateObj["CustomerId"] = settings.CustomerId;
            templateObj["UserId"] = settings.UserId;
            templateObj["Locale"] = "en";

            byte[] byteArray = Encoding.ASCII.GetBytes(templateObj.ToString());

            // create and send message            
            ServiceBusMessage message = new ServiceBusMessage(byteArray)
            {
                ContentType = "application/json"
            };
            
            await sender.SendMessageAsync(message);
        }

        [Theory]
        [InlineData("AssignSuperUserRole")]
        [InlineData("ContactRequest")]
        [InlineData("PortfolioAssigned")]
        [InlineData("SiteAssigned")]
        [InlineData("TicketAssigned")]
        [InlineData("TicketCreated")]
        [InlineData("TicketReassigned")]
        [InlineData("TicketResolved")]
        public async Task Trigger_emails_in_french(string templateFile)
        {
            var fileName = templateFile;

            // message payload
            var template = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Templates", String.Concat(fileName, ".json")));

            // inject values from config
            JObject templateObj = JObject.Parse(template);
            templateObj["CorrelationId"] = settings.CorrelationId;
            templateObj["CustomerId"] = settings.CustomerId;
            templateObj["UserId"] = settings.UserId;
            templateObj["Locale"] = "fr";

            byte[] byteArray = Encoding.ASCII.GetBytes(templateObj.ToString());

            // create and send message            
            ServiceBusMessage message = new ServiceBusMessage(byteArray)
            {
                ContentType = "application/json"
            };

            await sender.SendMessageAsync(message);
        }

        public async void Dispose()
        {
            await sender.DisposeAsync();
            await client.DisposeAsync();
        }
    }
}