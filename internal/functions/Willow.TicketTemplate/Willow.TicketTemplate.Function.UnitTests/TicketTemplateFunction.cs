using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Azure.Functions.Worker;

using Xunit;
using Moq;

using Newtonsoft.Json;

using Willow.Api.Client;
using Willow.Calendar;

using Willow.TicketTemplate.Function;
using Willow.Platform.Models;

namespace Willow.TicketTemplate.Function.UnitTests
{
    public class TicketTemplateFunctionTests
    {
        private readonly TicketTemplateFunction _function;
        private readonly Mock<IRestApi> _directoryCore;
        private readonly Mock<IRestApi> _workflowCore;
        private readonly Mock<FunctionContext> _context;
        private readonly Guid _customerId1 = Guid.NewGuid();
        private readonly Guid _customerId2 = Guid.NewGuid();
        private readonly Guid _customerId3 = Guid.NewGuid();

        public TicketTemplateFunctionTests()
        {
            _directoryCore = new Mock<IRestApi>();
            _workflowCore = new Mock<IRestApi>();
            _context = new Mock<FunctionContext>();

            var svc = new TicketTemplateService(_directoryCore.Object, _workflowCore.Object);

            _function = new TicketTemplateFunction(svc);
        }

        [Fact]
        public async Task TicketTemplateFunction_RunStart_success()
        {
            var correlationId = Guid.NewGuid();

            _directoryCore.Setup( d=> d.Get<List<Customer>>("customers?active=true", It.IsAny<object>())).ReturnsAsync(new List<Customer>
            {
                new Customer
                {
                    Id = _customerId1,
                    Name = "Fred",
                    Status = Customer.CustomerStatus.Active
                },
                new Customer
                {
                    Id = _customerId2,
                    Name = "Wilma",
                    Status = Customer.CustomerStatus.Active
                }
            });

            var result = await _function.RunStart("{ \"CorrelationId\": \"" + correlationId.ToString() + "\"}", _context.Object);

            Assert.NotNull(result); 
            Assert.Equal(2, result.Length); 
            Assert.Equal(_customerId1, result[0].CustomerId); 
            Assert.Equal(_customerId2, result[1].CustomerId); 
            Assert.Equal(correlationId, result[0].CorrelationId); 
            Assert.Equal(correlationId, result[1].CorrelationId); 
        }

        [Fact]
        public async Task TicketTemplateFunction_RunCustomer_success()
        {
            var correlationId = Guid.NewGuid();
            var siteId1 = Guid.NewGuid();
            var siteId2 = Guid.NewGuid();
            var siteId3 = Guid.NewGuid();

            _directoryCore.Setup( d=> d.Get<List<Site>>($"sites/customer/{_customerId1}?isScheduledTicketsEnabled=true", It.IsAny<object>())).ReturnsAsync(new List<Site>
            {
                new Site
                {
                    Id = siteId1,
                    Name = "Fred"
                },
                new Site
                {
                    Id = siteId2,
                    Name = "Wilma"
                },
                new Site
                {
                    Id = siteId3,
                    Name = "Pebbles"
                }
            });

            var msg = new {CorrelationId = correlationId, CustomerId = _customerId1 };
            var result = await _function.RunCustomer(JsonConvert.SerializeObject(msg), _context.Object);

            Assert.NotNull(result); 
            Assert.Equal(3, result.Length); 
            Assert.Equal(siteId1, result[0].SiteId); 
            Assert.Equal(siteId2, result[1].SiteId); 
            Assert.Equal(siteId3, result[2].SiteId); 
            Assert.Equal(_customerId1, result[0].CustomerId); 
            Assert.Equal(_customerId1, result[1].CustomerId); 
            Assert.Equal(_customerId1, result[2].CustomerId); 
            Assert.Equal(correlationId, result[0].CorrelationId); 
            Assert.Equal(correlationId, result[1].CorrelationId); 
            Assert.Equal(correlationId, result[2].CorrelationId); 
        }

        [Fact]
        public async Task TicketTemplateFunction_RunSite_success()
        {
            var correlationId = Guid.NewGuid();
            var siteId1 = Guid.NewGuid();
            var templateId1 = Guid.NewGuid();
            var templateId2 = Guid.NewGuid();
            var templateId3 = Guid.NewGuid();

            _workflowCore.Setup( d=> d.Get<List<ScheduleHit>>($"sites/{siteId1}/tickettemplate/schedules?correlationId={correlationId}", It.IsAny<object>())).ReturnsAsync(new List<ScheduleHit>
            {
                new ScheduleHit
                {
                    OwnerId = templateId1,
                    HitDate = DateTime.Now
                },
                new ScheduleHit
                {
                    OwnerId = templateId2,
                    HitDate = DateTime.Now
                },
                new ScheduleHit
                {
                    OwnerId = templateId3,
                    HitDate = DateTime.Now
                }
            });

            var msg = new {CorrelationId = correlationId, CustomerId = _customerId1, SiteId = siteId1 };
            var result = await _function.RunSite(JsonConvert.SerializeObject(msg), _context.Object);

            Assert.NotNull(result); 
            Assert.Equal(3, result.Length); 
            Assert.Equal(templateId1, result[0].TemplateId); 
            Assert.Equal(templateId2, result[1].TemplateId); 
            Assert.Equal(templateId3, result[2].TemplateId); 
        }

        [Fact]
        public async Task TicketTemplateFunction_RunTemplate_success()
        {
            var correlationId = Guid.NewGuid();
            var siteId1 = Guid.NewGuid();
            var templateId1 = Guid.NewGuid();
            var hitDate = DateTime.Now;
            var utcNow =  DateTime.UtcNow;
            var assetId1 = Guid.NewGuid();
            var assetId2 = Guid.NewGuid();
            var assetId3 = Guid.NewGuid();

            _workflowCore.Setup( d=> d.Post<ScheduleHit, List<ScheduledTicketAsset>>("tickettemplate/schedule/assets", It.IsAny<ScheduleHit>(), It.IsAny<object>())).ReturnsAsync(new List<ScheduledTicketAsset>
            {
                new ScheduledTicketAsset
                {
                    CorrelationId = correlationId,
                    TemplateId = templateId1,
                    AssetId = assetId1,
                    SequenceNumber = "1",
                    Occurrence = hitDate.Daydex(),
                    ScheduleHitDate = hitDate,
                    UtcNow = utcNow
                },
                new ScheduledTicketAsset
                {
                    CorrelationId = correlationId,
                    TemplateId = templateId1,
                    AssetId = assetId2,
                    SequenceNumber = "2",
                    Occurrence = hitDate.Daydex(),
                    ScheduleHitDate = hitDate,
                    UtcNow = utcNow
                },
                new ScheduledTicketAsset
                {
                    CorrelationId = correlationId,
                    TemplateId = templateId1,
                    AssetId = assetId3,
                    SequenceNumber = "3",
                    Occurrence = hitDate.Daydex(),
                    ScheduleHitDate = hitDate,
                    UtcNow = utcNow
                }
            });

            var msg = new {CorrelationId = correlationId, CustomerId = _customerId1, SiteId = siteId1, TemplateId = templateId1 };
            var result = await _function.RunTemplate(JsonConvert.SerializeObject(msg), _context.Object);

            Assert.NotNull(result); 
            Assert.Equal(3, result.Length); 
            Assert.Equal(templateId1, result[0].TemplateId); 
            Assert.Equal(assetId1, result[0].AssetId); 
            Assert.Equal(assetId1, result[0].AssetId); 
            Assert.Equal(assetId2, result[1].AssetId); 
            Assert.Equal(assetId3, result[2].AssetId); 
            
            Assert.Equal(hitDate.Daydex(), result[0].Occurrence); 
            Assert.Equal("1",              result[0].SequenceNumber); 
            Assert.Equal("2",              result[1].SequenceNumber); 
            Assert.Equal("3",              result[2].SequenceNumber); 
            Assert.Equal(hitDate,          result[0].ScheduleHitDate); 
            Assert.Equal(utcNow,           result[0].UtcNow); 
        }

        [Fact]
        public async Task TicketTemplateFunction_RunAsset_success()
        {
            var correlationId = Guid.NewGuid();
            var siteId1       = Guid.NewGuid();
            var templateId1   = Guid.NewGuid();
            var assetId1      = Guid.NewGuid();
            var utcNow        = DateTime.UtcNow;

            var msg = new AssetMessage
            {
                CorrelationId   = correlationId, 
                CustomerId      = _customerId1, 
                SiteId          = siteId1, 
                TemplateId      = templateId1,
                AssetId         = assetId1,
                AssetName       = "Bob",
                ScheduleHitDate = utcNow,
                Occurrence      = utcNow.Daydex(),
                UtcNow          = utcNow,
                SequenceNumber  = "123"
            };

            ScheduledTicketAsset sentAsset = null;

            _workflowCore.Setup( w=> w.PostCommand<ScheduledTicketAsset>("tickettemplate/schedule/asset", It.IsAny<ScheduledTicketAsset>(), null)).Callback<string, ScheduledTicketAsset, object>( (string Url, ScheduledTicketAsset asset, object headers)=> sentAsset = asset);

            await _function.RunAsset(JsonConvert.SerializeObject(msg), _context.Object);

            _workflowCore.Verify( w=> w.PostCommand<ScheduledTicketAsset>("tickettemplate/schedule/asset", It.IsAny<ScheduledTicketAsset>(), null), Times.Once);

            Assert.NotNull(sentAsset); 
            Assert.Equal(correlationId,   sentAsset.CorrelationId); 
            Assert.Equal(templateId1,     sentAsset.TemplateId); 
            Assert.Equal(assetId1,        sentAsset.Id); 
            Assert.Equal(assetId1,        sentAsset.AssetId); 
            Assert.Equal("Bob",           sentAsset.AssetName); 
            Assert.Equal("123",           sentAsset.SequenceNumber); 
            Assert.Equal(utcNow.Daydex(), sentAsset.Occurrence); 
            Assert.Equal(utcNow,          sentAsset.ScheduleHitDate); 
            Assert.Equal(utcNow,          sentAsset.UtcNow); 

        }
    }
}