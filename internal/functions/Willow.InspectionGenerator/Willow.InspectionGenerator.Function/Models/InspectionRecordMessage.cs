using System;

namespace Willow.InspectionGenerator.Function.Models
{
    public class InspectionRecordMessage: GenerateInspectionRecordMessage
    {
        public Guid InspectionRecordId { get; init; }
        public DateTime EffectiveDate { get; set; }
    }
}
