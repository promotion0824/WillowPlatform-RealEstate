using System;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;

using Willow.Functions.Common;
using Willow.PushNotification;

namespace Willow.Communications.Function
{
    public class PushInstallationFunction : BaseFunction
    {
        private readonly IPushNotificationService _pushNotificationService;
        private const string FunctionName = "PushInstallation";

        public PushInstallationFunction(IPushNotificationService pushNotificationService)
        {
            _pushNotificationService = pushNotificationService;
        }
        
        [Function(FunctionName)]
        public async Task Run([ServiceBusTrigger("pushinstallation", Connection = "ServiceBusConnectionString")] string input, FunctionContext executionContext)
        {
            await Invoke<PushInstallationMessage>(input, FunctionName, executionContext, async (message, log) =>
            {
                if (message.Action == "delete")
                {
                    await _pushNotificationService.DeleteInstallation(message.UserId, message.Handle);
                }
                else
                {
                    await _pushNotificationService.AddOrUpdateInstallation(message.UserId, message.Handle, message.Platform);
                }
            });
        }
    }

    public class PushInstallationMessage
    {
        public Guid UserId { get; set; }
        public string Handle { get; set; }
        public string Platform { get; set; }
        public string Action { get; set; }
    }
}

