using System;

namespace Willow.InspectionGenerator.Function.Models
{
    public class GenerateCheckRecordRequest
    {
        public Guid InspectionId { get; set; }
        public Guid InspectionRecordId { get; set; }
        public Guid CheckId { get; set; }
        public Guid SiteId { get; set; }
        public DateTime? EffectiveDate { get; set; }
    }
}
