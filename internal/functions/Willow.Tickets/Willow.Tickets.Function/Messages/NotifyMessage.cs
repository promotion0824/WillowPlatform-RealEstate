using System;

using Willow.Functions.Common;
using Willow.Platform.Models;

namespace Willow.Tickets.Function.Messages
{
    public class NotifyMessage : SiteMessage
    {
        public Guid   TicketId    { get; init; }
        public bool   OnCreate    { get; init; } // false for update
        public bool   Reassigned  { get; init; } // only for update
        public string SiteName    { get; init; }  = "";
        public string TicketUrl   { get; init; }  = "";
    }
}
