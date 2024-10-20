using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Willow.Api.Client;
using Willow.Platform.Models;
using Willow.Functions.Common;

namespace Willow.TicketTemplate.Function
{
    public interface ITicketTemplateService : IFunctionService
    {
        Task<TemplateMessage[]> GetTicketTemplates(SiteMessage msg);
        Task<AssetMessage[]>    GetTemplateAssets(TemplateMessage msg);
        Task                    CreateTicketForAsset(AssetMessage msg);
    }

    public class TicketTemplateService : BaseFunctionService, ITicketTemplateService
    {
        public TicketTemplateService(IRestApi directoryCore, IRestApi workflowCore)
                              : base(directoryCore, workflowCore)
        {
        }

        #region ITicketTemplateService

        public async Task<TemplateMessage[]> GetTicketTemplates(SiteMessage msg)
        {
            var schedules = await _workflowCore.Get<List<ScheduleHit>>($"sites/{msg.SiteId}/tickettemplate/schedules?correlationId={msg.CorrelationId}");

            return schedules.Select(s => new TemplateMessage
            {
                CorrelationId = msg.CorrelationId,  
                CustomerId    = msg.CustomerId,
                SiteId        = msg.SiteId,
                TemplateId    = s.OwnerId,
                HitDate       = s.HitDate
            }).ToArray();
        }

        public async Task<AssetMessage[]> GetTemplateAssets(TemplateMessage msg)
        {
            var assets = await _workflowCore.Post<ScheduleHit, List<ScheduledTicketAsset>>("tickettemplate/schedule/assets", new ScheduleHit
            {
                OwnerId = msg.TemplateId,
                HitDate = msg.HitDate
            });

            return assets.Select(s => new AssetMessage
            {
                CorrelationId   = msg.CorrelationId,  
                CustomerId      = msg.CustomerId,
                SiteId          = msg.SiteId,
                TemplateId      = msg.TemplateId,
                HitDate         = msg.HitDate,
                AssetId         = s.AssetId,
                AssetName       = s.AssetName,
                SequenceNumber  = s.SequenceNumber,
                Occurrence      = s.Occurrence,
                ScheduleHitDate = s.ScheduleHitDate,
                UtcNow          = s.UtcNow
            }).ToArray();
        }

        public Task CreateTicketForAsset(AssetMessage msg)
        {
            return _workflowCore.PostCommand<ScheduledTicketAsset>("tickettemplate/schedule/asset", new ScheduledTicketAsset
            {
                CorrelationId   = msg.CorrelationId,
                Id              = msg.AssetId,
                TemplateId      = msg.TemplateId,
                AssetId         = msg.AssetId,
                AssetName       = msg.AssetName,
                SequenceNumber  = msg.SequenceNumber,
                Occurrence      = msg.Occurrence,
                ScheduleHitDate = msg.ScheduleHitDate,
                UtcNow          = msg.UtcNow
            });
        }

        #endregion
    }
}
