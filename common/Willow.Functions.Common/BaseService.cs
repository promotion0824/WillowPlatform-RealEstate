using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Willow.Api.Client;
using Willow.Platform.Models;

namespace Willow.Functions.Common
{
    public interface IFunctionService
    {
        Task<CustomerMessage[]> GetCustomers(StartMessage msg);
        Task<SiteMessage[]>     GetSites(CustomerMessage msg, string filter = "");
    }

    public abstract class BaseFunctionService : IFunctionService
    {
        protected readonly IRestApi _directoryCore;
        protected readonly IRestApi _workflowCore;

        protected BaseFunctionService(IRestApi directoryCore, IRestApi workflowCore)
        {
            _directoryCore = directoryCore ?? throw new ArgumentNullException(nameof(directoryCore));
            _workflowCore  = workflowCore ?? throw new ArgumentNullException(nameof(workflowCore));
        }

        public async Task<CustomerMessage[]> GetCustomers(StartMessage msg)
        {
            var customers = await _directoryCore.Get<List<Customer>>("customers?active=true");

            return customers.Select(customer => new CustomerMessage
            {
                CorrelationId = msg.CorrelationId,
                CustomerId = customer.Id
            }).ToArray();
        }

        public async Task<SiteMessage[]> GetSites(CustomerMessage msg, string filter = "")
        {
            var url = $"sites/customer/{msg.CustomerId}{filter}";
            var sites = await _directoryCore.Get<List<Site>>(url);

            return sites.Select(site => new SiteMessage
            {
                CorrelationId = msg.CorrelationId,
                CustomerId    = msg.CustomerId,
                SiteId        = site.Id
            }).ToArray();
        }
   }
}
