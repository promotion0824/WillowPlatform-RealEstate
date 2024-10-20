using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Willow.Tickets.Function
{
    public class Workgroup 
    {
        public Guid         Id        { get; set; }
        public string       Name      { get; set; } = "";
        public Guid         SiteId    { get; set; }
        public List<Guid>   MemberIds { get; set; } = new List<Guid>();
    }
}
