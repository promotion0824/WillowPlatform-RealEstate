using System;

namespace Willow.InspectionGenerator.Function.Models
{
    public class GenerateInspectionRecordRequest
    {
        public Guid InspectionId { get; set; }
        public Guid SiteId { get; set; }
        public DateTime? HitTime { get; set; }
        public DateTime? SiteNow { get; set; }
    }
}
