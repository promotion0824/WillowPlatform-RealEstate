using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Willow.Api.Client;
using Willow.Functions.Common;
using Willow.Logging;
using Willow.Platform.Models;

namespace Willow.TicketTemplate.Function
{
    public class TicketTemplateFunction : BaseFunction
    {
        private const string FunctionName         = "TicketTemplate";
        private const string FunctionNameTimer    = $"{FunctionName}Timer";
        private const string FunctionNameStart    = $"{FunctionName}Start";
        private const string FunctionNameCustomer = $"{FunctionName}Customer";
        private const string FunctionNameSite     = $"{FunctionName}Site";
        private const string FunctionNameTemplate = $"{FunctionName}Template";
        private const string FunctionNameAsset    = $"{FunctionName}Asset";
                            
        private const string QueueName            = "ticket";
        private const string QueueNameStart       = $"{QueueName}start";
        private const string QueueNameCustomer    = $"{QueueName}customer";
        private const string QueueNameSite        = $"{QueueName}site";
        private const string QueueNameTemplate    = $"{QueueName}template";
        private const string QueueNameAsset       = $"{QueueName}asset";

        private const string ConnectionStringName = "ServiceBusConnectionString";

        private readonly ITicketTemplateService _service;

        public TicketTemplateFunction(ITicketTemplateService ticketTemplateService)
        {
            _service = ticketTemplateService;
        }

        [Function(FunctionNameTimer)]
        [ServiceBusOutput(QueueNameStart, Connection = ConnectionStringName)]
        public async Task<StartMessage> OnTimer([TimerTrigger("%TicketTemplateCron%")]TimerInfo timer, FunctionContext executionContext)
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
                var customers = await _service.GetCustomers(msg!);

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
                var sites = await _service.GetSites(msg!, "?isScheduledTicketsEnabled=true");

                log?.LogInformation($"{sites.Length} sites loaded");

                return sites;
            });
        }    

        [Function(FunctionNameSite)]
        [ServiceBusOutput(QueueNameTemplate, Connection = ConnectionStringName)]
        public Task<TemplateMessage[]?> RunSite([ServiceBusTrigger(QueueNameSite, Connection = ConnectionStringName)] string message, FunctionContext executionContext)
        {
            return Invoke<SiteMessage, TemplateMessage[]>(message, FunctionNameSite, executionContext, async (msg, log)=>
            { 
                var templates = await _service.GetTicketTemplates(msg!);

                log?.LogInformation($"{templates.Length} templates loaded");

                return templates;
            });
        }

        [Function(FunctionNameTemplate)]
        [ServiceBusOutput(QueueNameAsset, Connection = ConnectionStringName)]
        public Task<AssetMessage[]?> RunTemplate([ServiceBusTrigger(QueueNameTemplate, Connection = ConnectionStringName)] string message, FunctionContext executionContext)
        {
            return Invoke<TemplateMessage, AssetMessage[]>(message, FunctionNameTemplate, executionContext, async (msg, log)=>
            { 
                var assets = await _service.GetTemplateAssets(msg!);

                log?.LogInformation($"{assets.Length} assets loaded");

                return assets;
            });
        }

        [Function(FunctionNameAsset)]
        public async Task RunAsset([ServiceBusTrigger(QueueNameAsset, Connection = ConnectionStringName)] string message, FunctionContext executionContext)
        {
            await Invoke<AssetMessage, string>(message, FunctionNameAsset, executionContext, async (msg, log)=>
            { 
                await _service.CreateTicketForAsset(msg!);

                return "";
            });
        }
    }
}
