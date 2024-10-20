using System;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;

using Willow.Functions.Common;
using Willow.Tickets.Function.Messages;

namespace Willow.Tickets.Function
{
    /// <summary>
    /// Generates inspection records based on the schedule in the inspection 
    /// </summary>
    public class TicketFunction : BaseFunction
    {
        private const string FunctionName                       = "Ticket";
        private const string FunctionNameUpdateInsightStatus    = $"{FunctionName}UpdateInsightStatus";
        private const string FunctionNameBroadcastToMarketplace = $"{FunctionName}BroadcastToMarketplace";
        private const string FunctionNameNotify                 = $"{FunctionName}Notify";
        private const string FunctionNameRecipient              = $"{FunctionName}NotifyRecipient";

        private const string QueueName                          = "ticket";
        private const string QueueNameInsightStatus             = $"{QueueName}insightstatus";
        private const string QueueNameMarketplace               = $"{QueueName}marketplace";
        private const string QueueNameNotify                    = $"{QueueName}notify";
        private const string QueueNameNotifyRecipient           = $"{QueueName}notifyrecipient";
        private const string QueueNameCommSvc                   = "commsvc";

        private const string ConnectionStringName = "ServiceBusConnectionString";

        private readonly ITicketService _ticketService;

        public TicketFunction(ITicketService ticketService)
        {
            _ticketService = ticketService ?? throw new ArgumentNullException(nameof(ticketService));
        }

        [Function(FunctionNameUpdateInsightStatus)]
        public Task RunUpdateInsightStatus([ServiceBusTrigger(QueueNameInsightStatus, Connection = ConnectionStringName)] string message, FunctionContext executionContext)
        {
            return Invoke<InsightStatusMessage>(message, FunctionNameUpdateInsightStatus, executionContext, (msg, log) =>
            {
                return _ticketService.UpdateInsightStatus(msg!, log);
            });
        }

        [Function(FunctionNameBroadcastToMarketplace)]
        public Task RunBroadcastToMarketplace([ServiceBusTrigger(QueueNameMarketplace, Connection = ConnectionStringName)] string message, FunctionContext executionContext)
        {
            return  Invoke<MarketplaceMessage>(message, FunctionNameBroadcastToMarketplace, executionContext, (msg, log) =>
            {
                return _ticketService.BroadcastToMarketplace(msg!, log);
            });
        }

        [Function(FunctionNameNotify)]
        [ServiceBusOutput(QueueNameNotifyRecipient, Connection = ConnectionStringName)]
        public Task<NotifyRecipientMessage[]?> RunNotify([ServiceBusTrigger(QueueNameNotify, Connection = ConnectionStringName)] string message, FunctionContext executionContext)
        {
            return Invoke<NotifyMessage, NotifyRecipientMessage[]>(message, FunctionNameNotify, executionContext, (msg, log) =>
            {
                return _ticketService.NotifyRecipients(msg!, log);
            });
        }

        [Function(FunctionNameRecipient)]
        [ServiceBusOutput(QueueNameCommSvc, Connection = ConnectionStringName)]
        public Task<NotificationMessage[]?> RunNotifyRecipient ([ServiceBusTrigger(QueueNameNotifyRecipient, Connection = ConnectionStringName)] string message, FunctionContext executionContext)
        {
            return Invoke<NotifyRecipientMessage, NotificationMessage[]>(message, FunctionNameRecipient, executionContext, (msg, log) =>
            {
                return _ticketService.NotifyRecipient(msg!, log);
            });
        }
    }
}
