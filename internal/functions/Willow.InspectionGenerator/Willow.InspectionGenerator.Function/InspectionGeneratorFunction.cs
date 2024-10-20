using System;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Willow.Functions.Common;
using Willow.InspectionGenerator.Function.Models;

namespace Willow.InspectionGenerator.Function
{
    /// <summary>
    /// Generates inspection records based on the schedule in the inspection 
    /// </summary>
    public class InspectionGeneratorFunction : BaseFunction
    {
        private const string FunctionName                   = "InspectionGenerator";
        private const string FunctionNameTimer              = $"{FunctionName}Timer";
        private const string FunctionNameStart              = $"{FunctionName}Start";
        private const string FunctionNameCustomer           = $"{FunctionName}Customer";
        private const string FunctionNameSite               = $"{FunctionName}Site";
        private const string FunctionNameCreateRecord       = $"{FunctionName}CreateRecord";
        private const string FunctionNameChecks             = $"{FunctionName}Checks";
        private const string FunctionNameCreateCheck        = $"{FunctionName}CreateCheck";

        private const string QueueName                      = "inspectiongenerator";
        private const string QueueNameStart                 = $"{QueueName}start";
        private const string QueueNameCustomer              = $"{QueueName}customer";
        private const string QueueNameSite                  = $"{QueueName}site";
        private const string QueueNameCreateRecord          = $"{QueueName}createrecord";
        private const string QueueNameChecks                = $"{QueueName}checks";
        private const string QueueNameCreateCheck           = $"{QueueName}createcheck";

        private const string ConnectionStringName = "ServiceBusConnectionString";

        private readonly IInspectionGeneratorService _generatorService;

        public InspectionGeneratorFunction(IInspectionGeneratorService generatorService)
        {
            _generatorService = generatorService ?? throw new ArgumentNullException(nameof(generatorService));
        }

        // InspectionGeneratorTimer - Entry Function
        // -> InspectionGeneratorStart - Loads the list of customers and iterates over them and sends a message for each one
        //    -> InspectionGeneratorCustomer - Loads all sites for the customer and sends a message for each one
        //       -> InspectionGeneratorSite - Loads all inspections for the site and sends a message for each one
        //          -> InspectionGeneratorRecordCreator - Creates an inspection record and sends a message for each inspection record check
        //              -> InspectionGeneratorChecks - Iterates over checks and sends message to create check record.
        //                  -> InspectionGeneratorCheckRecordCreator - Creates a check record

        [Function(FunctionNameTimer)]
        [ServiceBusOutput(QueueNameStart, Connection = ConnectionStringName)]
        public async Task<StartMessage> OnTimer([TimerTrigger("%InspectionGeneraterCron%")] TimerInfo timer, FunctionContext executionContext)
        {
            var outputMsg = new StartMessage();

            await Log(outputMsg, FunctionNameTimer, executionContext, "timer");

            return outputMsg;
        }

        [Function(FunctionNameStart)]
        [ServiceBusOutput(QueueNameCustomer, Connection = ConnectionStringName)]
        public async Task<CustomerMessage[]?> RunStart([ServiceBusTrigger(QueueNameStart, Connection = ConnectionStringName)] string message, FunctionContext executionContext)
        {
            return await Invoke<StartMessage, CustomerMessage[]>(message, FunctionNameStart, executionContext, async (msg, log) =>
            {
                var customers = await _generatorService.GetCustomers(msg!);

                log?.LogInformation($"{customers.Length} customers loaded");

                return customers;
            });
        }

        [Function(FunctionNameCustomer)]
        [ServiceBusOutput(QueueNameSite, Connection = ConnectionStringName)]
        public Task<SiteMessage[]?> RunCustomer([ServiceBusTrigger(QueueNameCustomer, Connection = ConnectionStringName)] string message, FunctionContext executionContext)
        {
            return Invoke<CustomerMessage, SiteMessage[]>(message, FunctionNameCustomer, executionContext, async (msg, log) =>
            {
                var sites = await _generatorService.GetSites(msg!, "?isInspectionEnabled=true");

                log?.LogInformation($"{sites.Length} sites loaded");

                return sites;
            });
        }

        [Function(FunctionNameSite)]
        [ServiceBusOutput(QueueNameCreateRecord, Connection = ConnectionStringName)]
        public Task<GenerateInspectionRecordMessage[]?> RunSite([ServiceBusTrigger(QueueNameSite, Connection = ConnectionStringName)] string message, FunctionContext executionContext)
        {
            return Invoke<SiteMessage, GenerateInspectionRecordMessage[]>(message, FunctionNameSite, executionContext, async (msg, log) =>
            {
                var scheduledInspections = await _generatorService.GetScheduledInspections(msg!);

                log?.LogInformation($"{scheduledInspections.Length} inspections loaded");

                return scheduledInspections;
            });
        }

        [Function(FunctionNameCreateRecord)]
        [ServiceBusOutput(QueueNameChecks, Connection = ConnectionStringName)]
        public Task<InspectionRecordMessage?> RunCreateRecord([ServiceBusTrigger(QueueNameCreateRecord, Connection = ConnectionStringName)] string message, FunctionContext executionContext)
        {
            return Invoke<GenerateInspectionRecordMessage, InspectionRecordMessage>(message, FunctionNameCreateRecord, executionContext, async (msg, log) =>
                await _generatorService.GenerateInspectionRecord(msg!));
        }

        [Function(FunctionNameChecks)]
        [ServiceBusOutput(QueueNameCreateCheck, Connection = ConnectionStringName)]
        public Task<GenerateCheckRecordMessage[]?> RunChecks([ServiceBusTrigger(QueueNameChecks, Connection = ConnectionStringName)] string message, FunctionContext executionContext)
        {
            return Invoke<InspectionRecordMessage, GenerateCheckRecordMessage[]>(message, FunctionNameChecks, executionContext, async (msg, log) =>
            {
                var checks = await _generatorService.GetScheduledChecks(msg!);

                log?.LogInformation($"{checks.Length} checks loaded");

                return checks;
            });
        }

        [Function(FunctionNameCreateCheck)]
        public Task RunCreateCheck([ServiceBusTrigger(QueueNameCreateCheck, Connection = ConnectionStringName)] string message, FunctionContext executionContext)
        {
            return Invoke<GenerateCheckRecordMessage>(message, FunctionNameCreateCheck, executionContext, async (msg, log) =>
            {
                var generatedCheck = await _generatorService.GenerateCheckRecord(msg!);

                if (generatedCheck != null)
                {
                    log?.LogInformation("{Id}: {Status} check generated", generatedCheck.Id, generatedCheck.Status);
                }
                else
                {
                    log?.LogInformation("Message {@message}, didn't generate check", message);
                }
            });
        }
    }
}
