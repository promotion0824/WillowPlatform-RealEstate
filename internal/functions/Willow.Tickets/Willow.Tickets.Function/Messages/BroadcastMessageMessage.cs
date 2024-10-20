using System;

using Willow.Functions.Common;
using Willow.Platform.Models;

namespace Willow.Tickets.Function.Messages
{
    public class BroadcastToMarketplaceMessage : BaseMessage
    {
        public Guid SiteId { get; init; }
        public Guid TicketId { get; init; }
        public AppMessageType Type { get; init; }
    }
}
