using System;

using Willow.Functions.Common;
using Willow.Platform.Models;

namespace Willow.Tickets.Function.Messages
{
    public class NotifyRecipientMessage : SiteMessage
    {
        public Guid   TicketId       { get; init; }
        public Guid   RecipientId    { get; init; }
        public bool   OnCreate       { get; init; } // false for update
        public string SiteName       { get; init; }  = "";
        public string SequenceNumber { get; init; } = "";
        public string TicketSummary  { get; init; } = "";
        public string TicketUrl      { get; init; }  = "";
        public string TemplateName   { get; init; }  = "";
    }
}
