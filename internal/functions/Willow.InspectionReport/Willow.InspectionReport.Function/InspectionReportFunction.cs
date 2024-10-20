using System;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using Willow.Functions.Common;
using Willow.Logging;

namespace Willow.InspectionReport.Function
{
    /// <summary>
    /// Generates inspection records based on the schedule in the inspection 
    /// </summary>
    public class InspectionReportFunction : BaseFunction
    {
        private const string FunctionName         = "InspectionReport";
        private const string FunctionNameTimer    = $"{FunctionName}Timer";
        private const string FunctionNameStart    = $"{FunctionName}Start";
        private const string FunctionNameCustomer = $"{FunctionName}Customer";
        private const string FunctionNameSite     = $"{FunctionName}Site";
                            
        private const string QueueName            = "inspectionreport";
        private const string QueueNameStart       = $"{QueueName}start";
        private const string QueueNameCustomer    = $"{QueueName}customer";
        private const string QueueNameSite        = $"{QueueName}site";

        private const string ConnectionStringName = "ServiceBusConnectionString";

        private readonly IInspectionReportService _reportService;

        public InspectionReportFunction(IInspectionReportService reportService)
        {
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        }

        // InspectionReportTimer - Entry Function
        // -> InspectionReportStart - Loads the list of customers and iterates over them and sends a message for each one
        //    -> InspectionReportCustomer - Loads all sites for the customer and sends a message for each one
        //       -> InspectionReportSite - Creates the daily inspection report and sends it

        [Function(FunctionNameTimer)]
        [ServiceBusOutput(QueueNameStart, Connection = ConnectionStringName)]
        public async Task<StartMessage> OnTimer([TimerTrigger("%InspectionReportCron%")]TimerInfo timer, FunctionContext executionContext)
        {
            var outputMsg = new StartMessage();

            await Log(outputMsg, FunctionNameTimer, executionContext, "timer");

            return outputMsg;
        }

        [Function(FunctionNameStart)]
        [ServiceBusOutput(QueueNameCustomer, Connection = ConnectionStringName)]
        public Task<CustomerMessage[]?> RunStart([ServiceBusTrigger(QueueNameStart, Connection = ConnectionStringName)] string message, FunctionContext executionContext)
        {
            return Invoke<StartMessage, CustomerMessage[]>(message, FunctionNameStart, executionContext, async (msg, log)=>
            { 
                var customers = await _reportService.GetCustomers(msg);

                log?.LogInformation($"{customers.Length} customers loaded");

                return customers;
            });
        }

        [Function(FunctionNameCustomer)]
        [ServiceBusOutput(QueueNameSite, Connection = ConnectionStringName)]
        public Task<SiteMessage[]?> RunCustomer([ServiceBusTrigger(QueueNameCustomer, Connection = ConnectionStringName)] string message, FunctionContext executionContext)
        {
            return Invoke<CustomerMessage, SiteMessage[]>(message, FunctionNameCustomer, executionContext, async (msg, log)=>
            { 
                var sites = await _reportService.GetSites(msg!, "?isInspectionEnabled=true");

                log?.LogInformation($"{sites.Length} sites loaded");

                return sites;
            });
        }

        [Function(FunctionNameSite)]
        public Task RunSite([ServiceBusTrigger(QueueNameSite, Connection = ConnectionStringName)] string message, FunctionContext executionContext)
        {
            return Invoke<SiteMessage>(message, FunctionNameSite, executionContext, (msg, log) =>
            { 
                return _reportService.SendReport(msg);
            });
        }
    }
}
