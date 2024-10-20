using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Willow.Api.Client;
using Willow.Common;
using Willow.Platform.Models;
using Willow.Tickets.Function.Messages;
using Willow.Functions.Common;

namespace Willow.Tickets.Function
{
    public interface ITicketService
    {
        Task UpdateInsightStatus(InsightStatusMessage msg, ILogger? log = null);
        Task BroadcastToMarketplace(MarketplaceMessage msg, ILogger? log = null);

        Task<NotifyRecipientMessage[]?> NotifyRecipients(NotifyMessage msg, ILogger? log = null);
        Task<NotificationMessage[]?> NotifyRecipient(NotifyRecipientMessage msg, ILogger? log = null);
    }

    public class TicketService : ITicketService
    {
        private readonly IRestApi _insightApi;
        private readonly IRestApi _marketplaceApi;
        private readonly IRestApi _directoryCore;
        private readonly IRestApi _workflowCore;
        public TicketService(IRestApi insightApi, IRestApi marketplaceApi, IRestApi directoryCore, IRestApi workflowCore)
        {
            _insightApi = insightApi ?? throw new ArgumentNullException(nameof(insightApi));
            _marketplaceApi = marketplaceApi ?? throw new ArgumentNullException(nameof(marketplaceApi));
            _directoryCore = directoryCore ?? throw new ArgumentNullException(nameof(directoryCore));
            _workflowCore = workflowCore ?? throw new ArgumentNullException(nameof(workflowCore));
        }

        #region ITicketService

        public Task UpdateInsightStatus(InsightStatusMessage msg, ILogger? log = null)
        {
            var url = $"sites/{msg.SiteId}/insights/{msg.InsightId}";
            var request = new UpdateInsightStatusRequest { Status = msg.Status ,LastStatus = msg.LastStatus,UpdatedByUserId = msg.UpdatedByUserId,SourceId = msg.SourceId};

            return _insightApi.PutCommand(url, request);
        }

        public Task BroadcastToMarketplace(MarketplaceMessage msg, ILogger? log = null)
        {
            var url = msg.AppId.HasValue ? $"apps/{msg.AppId}/messages" : "messages";
            var request = new BroadcastToMarketplaceRequest
            {
                SiteId = msg.SiteId,
                Type = msg.Type,
                Payload = new BroadcastToMarketplacePayload 
                { 
                    TicketId = msg.TicketId, 
                    InsightId = msg.InsightId 
                }
            };

            return _marketplaceApi.PostCommand(url, request);
        }

        public async Task<NotifyRecipientMessage[]?> NotifyRecipients(NotifyMessage msg, ILogger? log = null)
        {
            var ticket  = await _workflowCore.Get<Ticket>($"sites/{msg.SiteId}/tickets/{msg.TicketId}");
            var userIds = await GetAssigneeIds(ticket);

            return userIds.Select(id => new NotifyRecipientMessage
            {
                CorrelationId  = msg.CorrelationId,
                CustomerId     = msg.CustomerId,
                SiteId         = msg.SiteId,
                SiteName       = msg.SiteName,
                TicketId       = ticket.Id,
                RecipientId    = id,
                OnCreate       = msg.OnCreate,
                SequenceNumber = ticket.SequenceNumber,
                TicketSummary  = ticket.Summary,
                TicketUrl      = msg.TicketUrl,
                TemplateName   = GetTemplate(ticket.AssigneeType, msg.OnCreate, msg.Reassigned)

            }).ToArray();
        }

        public async Task<NotificationMessage[]?> NotifyRecipient(NotifyRecipientMessage msg, ILogger? log = null)
        {
            var parameters = new Dictionary<string, string>
            {
                { "TicketSequenceNumber", msg.SequenceNumber },
                { "TicketSummary",        msg.TicketSummary },
                { "SiteName",             msg.SiteName },
                { "TicketUrl",            msg.TicketUrl }
            };

            var email = new NotificationMessage
            {
                CorrelationId     = msg.CorrelationId,
                CustomerId        = msg.CustomerId,
                UserId            = msg.RecipientId,
                CommunicationType = "email",
                TemplateName      = msg.TemplateName,
                Data              = parameters
            };

            var result = new List<NotificationMessage> { email };
            var userPreferences = await _directoryCore.Get<CustomerUserPreferences>($"customers/{msg.CustomerId}/users/{msg.RecipientId}/preferences");

            if(userPreferences?.MobileNotificationEnabled ?? false)
            {
                result.Add(new NotificationMessage  
                {
                    CorrelationId      = msg.CorrelationId,
                    CustomerId         = msg.CustomerId,
                    UserId             = msg.RecipientId,
                    CommunicationType  = "pushnotification",
                    TemplateName       = msg.TemplateName,
                    Data               = parameters
                });
            }

            return result.ToArray();
        }

        #endregion

        #region Private

        private async Task<List<Guid>> GetAssigneeIds(Ticket ticket)
        {
            if (ticket.AssigneeType == AssigneeType.NoAssignee)
            {
                var assignees = await _workflowCore.Get<List<NotificationReceiver>>($"sites/{ticket.SiteId}/notificationReceivers");

                return assignees.Select(x => x.UserId).ToList();
            }

            if(!ticket.AssigneeId.HasValue)
            { 
                return new List<Guid>();
            }

            if (ticket.AssigneeType == AssigneeType.WorkGroup)
            {
                var workGroup  = await _workflowCore.Get<Workgroup>($"sites/{ticket.SiteId}/workgroups/{ticket.AssigneeId}");

                return workGroup.MemberIds;
            }

            return new List<Guid> { ticket.AssigneeId.Value };
        }

        private static string GetTemplate(AssigneeType assigneeType, bool isNewTicket, bool reAssigned)
        {                              
            if(isNewTicket)
            { 
                return assigneeType == AssigneeType.NoAssignee ? CommSvc.Templates.Email.Tickets.Created 
                                                               : CommSvc.Templates.Email.Tickets.Assigned;
            }

            return reAssigned ? CommSvc.Templates.Email.Tickets.Reassigned 
                              : CommSvc.Templates.Email.Tickets.Updated;
        }

        #endregion
    }

    public class UpdateInsightStatusRequest
    {
        public OldInsightStatus? Status { get; init; }
        public InsightStatus? LastStatus { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public Guid? SourceId { get; set; }
    }

    public class BroadcastToMarketplaceRequest
    {
        public Guid SiteId { get; init; }
        public AppMessageType Type { get; init; }
        public BroadcastToMarketplacePayload Payload { get; init; }

    }

    public class BroadcastToMarketplacePayload
    {
        public Guid? TicketId { get; init; }
        public Guid? InsightId { get; init; }
    }

    public static class CommSvc
    {
        public static class Templates
        {
            public static class Email
            {
                public static class Tickets
                {
                    public const string Created    = "TicketCreated";
                    public const string Assigned   = "TicketAssigned";
                    public const string Reassigned = "TicketReassigned";
                    public const string Updated    = "TicketUpdated";
                }
            }
        }
    }

}

