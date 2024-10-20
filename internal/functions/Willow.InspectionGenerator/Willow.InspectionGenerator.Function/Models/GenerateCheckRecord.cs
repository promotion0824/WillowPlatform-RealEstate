using System;

namespace Willow.InspectionGenerator.Function.Models
{
    public class GenerateCheckRecord
    {
        public Guid Id { get; set; }
        public Guid LastRecordId { get; set; }
        public CheckRecordStatus Status { get; set; }
    }
}
