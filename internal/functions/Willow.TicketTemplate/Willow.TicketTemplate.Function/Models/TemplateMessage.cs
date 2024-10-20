using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Willow.Functions.Common;

namespace Willow.TicketTemplate.Function
{
    public class TemplateMessage : SiteMessage
    {
        public Guid     TemplateId { get; init; }
        public DateTime HitDate    { get; init; }
    }

    public class AssetMessage : TemplateMessage
    {
        public Guid     AssetId         { get; init; }
        public string?  AssetName       { get; init; }
        public string?  SequenceNumber  { get; init; }
        public int      Occurrence      { get; init; }
        public DateTime ScheduleHitDate { get; init; }
        public DateTime UtcNow          { get; init; }
   }

    public class NotifyMessage : AssetMessage
    {
        public Guid TicketId { get; init; }
    }
}
