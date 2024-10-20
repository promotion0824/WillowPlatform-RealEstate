using System;

using Willow.Functions.Common;

namespace Willow.InspectionGenerator.Function.Models
{
    public class GenerateInspectionRecordMessage : SiteMessage
    {
        public Guid InspectionId { get; init; }
        public DateTime? SiteNow { get; set; }
    }
}
