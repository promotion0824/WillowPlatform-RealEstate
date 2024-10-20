using System;

namespace Willow.InspectionGenerator.Function.Models
{
    public class GenerateCheckRecordMessage : GenerateInspectionRecordMessage
    {
        public Guid CheckId { get; init; }
        public Guid InspectionRecordId { get; init; }
        public DateTime? EffectiveDate { get; set; }
    }
}
