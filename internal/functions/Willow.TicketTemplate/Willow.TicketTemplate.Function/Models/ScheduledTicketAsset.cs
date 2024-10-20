using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Willow.TicketTemplate.Function
{
    public class ScheduledTicketAsset
    {
        public Guid     CorrelationId   { get; set; }
        public Guid     Id              { get; set; }
        public Guid     TemplateId      { get; set; }
        public Guid     AssetId         { get; set; }
        public string?  AssetName       { get; set; }
        public string?  SequenceNumber  { get; set; }
        public int      Occurrence      { get; set; }
        public DateTime ScheduleHitDate { get; set; }
        public DateTime UtcNow          { get; set; }
    }
}
