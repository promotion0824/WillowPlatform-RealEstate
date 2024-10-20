using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;

using Willow.Communications.Service;
using Willow.Functions.Common;
using Willow.Platform.Models;

namespace Willow.Communications.Function
{
    public class CommunicationsServiceFunction : BaseFunction
    {
        private readonly ICommunicationsService _commService;
        private const string FunctionName = "CommunicationsService";

        public CommunicationsServiceFunction(ICommunicationsService commService)
        {
            _commService = commService;
        }

        [Function(FunctionName)]
        public async Task Run([ServiceBusTrigger("commsvc", Connection = "ServiceBusConnectionString")] string input, FunctionContext executionContext)
        {
            await Invoke<NotificationMessage>(input, FunctionName, executionContext, async (message, log) =>
            {
                await _commService.SendNotification(message.CustomerId,
                                                    message.UserId,
                                                    message.UserType.ToUserType(),
                                                    message.TemplateName,
                                                    message.Locale,
                                                    message.Data,
                                                    message.Tags,
                                                    message.CommunicationType);
            });
        }
    }
}

