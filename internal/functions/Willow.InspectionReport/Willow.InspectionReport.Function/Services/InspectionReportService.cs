using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Willow.Api.Client;
using Willow.Functions.Common;
using Willow.Platform.Models;

namespace Willow.InspectionReport.Function
{
    public interface IInspectionReportService : IFunctionService
    {
        Task SendReport(SiteMessage msg);
    }

    public class InspectionReportService : BaseFunctionService, IInspectionReportService
    {
        public InspectionReportService(IRestApi directoryCore, IRestApi workflowCore)
                                : base(directoryCore, workflowCore)
        {
        }

        public async Task SendReport(SiteMessage msg)
        {
            var url = $"inspections/reports/site/{msg.SiteId}";
                
            await _workflowCore.Post<string>(url);
        }
    }
}
