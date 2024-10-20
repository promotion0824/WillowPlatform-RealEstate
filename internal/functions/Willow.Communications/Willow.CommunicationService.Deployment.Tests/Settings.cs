using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Willow.CommunicationService.Deployment.Tests
{
    public class Settings
    {
        public string ServiceBusConnectionString {  get; set; }
        public string CorrelationId {  get; set; }
        public string CustomerId {  get; set; }
        public string UserId {  get; set; }
    }
}
