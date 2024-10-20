using System;

using Willow.Functions.Common;
using Willow.Platform.Models;

namespace Willow.Tickets.Function.Messages
{
    public class InsightStatusMessage : SiteMessage
    {
        public Guid InsightId { get; init; }
        public Guid? UpdatedByUserId { get; init; }
        public Guid? SourceId { get; set; }
        public OldInsightStatus? Status { get; init; }
        public InsightStatus? LastStatus { get; init; }
    }
}
