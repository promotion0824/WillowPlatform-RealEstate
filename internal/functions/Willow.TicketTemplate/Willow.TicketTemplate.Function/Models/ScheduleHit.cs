using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Willow.TicketTemplate.Function
{
    public class ScheduleHit
    {
        public Guid     ScheduleId { get; set; }
        public Guid     OwnerId    { get; set; }
        public DateTime HitDate    { get; set; }
        public string   EventName  { get; set; }
    }
}
