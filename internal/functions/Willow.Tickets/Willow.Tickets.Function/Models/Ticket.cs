using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Willow.Tickets.Function
{
    public class Ticket 
    {
        public Guid         Id             { get; set; }
        public Guid         CustomerId     { get; set; }
        public Guid         SiteId         { get; set; }
        public AssigneeType AssigneeType   { get; set; }
        public Guid?        AssigneeId     { get; set; }
        public string       SequenceNumber { get; init; } = "";
        public string       Summary        { get; init; } = "";
    }

     public enum AssigneeType
    {
        NoAssignee = 0,
        CustomerUser = 2,
        WorkGroup = 3,
    }
}
