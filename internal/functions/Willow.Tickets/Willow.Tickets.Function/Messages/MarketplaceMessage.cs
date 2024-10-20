using System;
using Willow.Functions.Common;
using Willow.Platform.Models;

namespace Willow.Tickets.Function.Messages
{
    public class MarketplaceMessage : SiteMessage
    {
        public Guid? InsightId { get; init; }
        public Guid? TicketId { get; init; }
        public AppMessageType Type { get; init; }
        public Guid? AppId { get; init; }
    }
}
