using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Willow.Api.Client;
using Willow.Common;
using Willow.Functions.Common;
using Willow.InspectionGenerator.Function.Models;

namespace Willow.InspectionGenerator.Function
{
    public interface IInspectionGeneratorService : IFunctionService
    {
        Task<GenerateInspectionRecordMessage[]> GetScheduledInspections(SiteMessage msg);
        Task<InspectionRecordMessage?> GenerateInspectionRecord(GenerateInspectionRecordMessage msg);
        Task<GenerateCheckRecordMessage?[]> GetScheduledChecks(InspectionRecordMessage msg);
        Task<GenerateCheckRecord?> GenerateCheckRecord(GenerateCheckRecordMessage msg);
    }

    public class InspectionGeneratorService : BaseFunctionService, IInspectionGeneratorService
    {
        private readonly IDateTimeService _dateTimeService;

        public InspectionGeneratorService(IDateTimeService dateTimeService, IRestApi directoryCore, IRestApi workflowCore)
                                : base(directoryCore, workflowCore)
        {
            _dateTimeService = dateTimeService;
        }

        public async Task<GenerateInspectionRecordMessage[]> GetScheduledInspections(SiteMessage msg)
        {
            var inspections = await _workflowCore.Get<List<GenerateInspectionRecord>>($"scheduledinspections/site/{msg.SiteId}");

            return inspections.Select(inspection => new GenerateInspectionRecordMessage
            {
                CorrelationId = msg.CorrelationId,
                CustomerId = msg.CustomerId,
                SiteId = msg.SiteId,
                InspectionId = inspection.Id,
                SiteNow = inspection.SiteNow
            }).ToArray();
        }

        public async Task<InspectionRecordMessage?> GenerateInspectionRecord(GenerateInspectionRecordMessage msg)
        {
            var url = "scheduledinspection/generate";
            var utcNow = _dateTimeService.UtcNow;
            var effectiveDate = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, 0, 0);

            var inspectionRecord = await _workflowCore.Post<GenerateInspectionRecordRequest, InspectionRecord>(url, new GenerateInspectionRecordRequest
            {
                HitTime = effectiveDate,
                InspectionId = msg.InspectionId,
                SiteId = msg.SiteId,
                SiteNow = msg.SiteNow
            });

            if (inspectionRecord == null)
            {
                return null;
            }

            return new InspectionRecordMessage
            {
                CorrelationId = msg.CorrelationId,
                CustomerId = msg.CustomerId,
                InspectionId = msg.InspectionId,
                SiteId = msg.SiteId,
                SiteNow = msg.SiteNow,
                EffectiveDate = effectiveDate,
                InspectionRecordId = inspectionRecord.Id
            };
        }

        public async Task<GenerateCheckRecordMessage[]> GetScheduledChecks(InspectionRecordMessage msg)
        {
            var inspection = await _workflowCore.Get<Inspection>($"sites/{msg.SiteId}/inspections/{msg.InspectionId}");

            return inspection.Checks.Select(check => new GenerateCheckRecordMessage
            {
                CorrelationId = msg.CorrelationId,
                CustomerId = msg.CustomerId,
                SiteId = msg.SiteId,
                InspectionId = msg.InspectionId,
                SiteNow = msg.SiteNow,
                EffectiveDate = msg.EffectiveDate,
                InspectionRecordId = msg.InspectionRecordId,
                CheckId = check.Id
            }).ToArray();
        }

        public async Task<GenerateCheckRecord?> GenerateCheckRecord(GenerateCheckRecordMessage msg)
        {
            var url = "scheduledinspection/generate/check";

            return await _workflowCore.Post<GenerateCheckRecordRequest, GenerateCheckRecord>(url, new GenerateCheckRecordRequest
            {
                CheckId = msg.CheckId,
                EffectiveDate = msg.EffectiveDate,
                InspectionId = msg.InspectionId,
                InspectionRecordId = msg.InspectionRecordId,
                SiteId = msg.SiteId
            });
        }
    }
}
