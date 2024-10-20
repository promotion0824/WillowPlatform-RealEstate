using System;
using System.Collections.Generic;

namespace Willow.InspectionGenerator.Function.Models
{
    public class Inspection
    {
        public Guid Id { get; set; }
        public List<Check> Checks { get; set; }
    }
}
