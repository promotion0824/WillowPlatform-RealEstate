using System;

namespace Willow.InspectionGenerator.Function.Models
{
    public class GenerateInspectionRecord
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime? SiteNow { get; set; }
    }
}
