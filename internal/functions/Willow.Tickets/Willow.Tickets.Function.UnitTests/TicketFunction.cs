using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moq;
using Xunit;

using Newtonsoft.Json;

using Willow.Api.Client;
using Willow.Platform.Mocks;
using Willow.Platform.Models;
using Willow.Tickets.Function.Messages;

namespace Willow.Tickets.Function.UnitTests
{
    public class TicketFunctionTests
    {
        private readonly TicketFunction _function;
        private readonly ITicketService _svc;
        private readonly FakeLogger _logger;
        private readonly Mock<IRestApi> _insightApi;
        private readonly Mock<IRestApi> _marketplaceApi;
        private readonly Mock<IRestApi> _workflowApi;
        private readonly Mock<IRestApi> _directoryApi;
        private readonly Mock<FunctionContext> _context;

        private readonly Guid _correlationId = Guid.NewGuid();  
        private readonly Guid _customerId   = Guid.NewGuid();  
        private readonly Guid _siteId       = Guid.NewGuid();  

        public TicketFunctionTests()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<ILoggerFactory, FakeLoggerFactory>();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            _context = new Mock<FunctionContext>();
            _context.SetupProperty(c => c.InstanceServices, serviceProvider);
            _logger = (serviceProvider.GetRequiredService<ILoggerFactory>() as FakeLoggerFactory).GetFakeLogger();

            _insightApi = new Mock<IRestApi>();
            _marketplaceApi = new Mock<IRestApi>();
            _workflowApi = new Mock<IRestApi>();
            _directoryApi = new Mock<IRestApi>();
            _svc = new TicketService(_insightApi.Object, _marketplaceApi.Object, _directoryApi.Object, _workflowApi.Object);
            _function = new TicketFunction(_svc);
        }

        #region RunUpdateInsightStatus

        [Fact]
        public async Task TicketFunction_RunUpdateInsightOldStatus_success()
        {
            var msg = new InsightStatusMessage 
            { 
                SiteId = Guid.NewGuid(), 
                InsightId = Guid.NewGuid(), 
                Status = OldInsightStatus.InProgress 
            };

            await _function.RunUpdateInsightStatus(JsonConvert.SerializeObject(msg), _context.Object);

            Assert.Equal("TicketUpdateInsightStatus", _logger.Entries[0].Properties["FunctionName"]);

            _insightApi.Verify(x => x.PutCommand($"sites/{msg.SiteId}/insights/{msg.InsightId}", 
                It.Is<UpdateInsightStatusRequest>(x => 
                    x.Status == msg.Status), 
                null), Times.Once);
        }

        [Fact]
        public async Task TicketFunction_RunUpdateInsightNewStatus_WithUserId_success()
        {
            var msg = new InsightStatusMessage
            {
                SiteId = Guid.NewGuid(),
                InsightId = Guid.NewGuid(),
                LastStatus = InsightStatus.Resolved,
                UpdatedByUserId = Guid.NewGuid()
            };

            await _function.RunUpdateInsightStatus(JsonConvert.SerializeObject(msg), _context.Object);

            Assert.Equal("TicketUpdateInsightStatus", _logger.Entries[0].Properties["FunctionName"]);

            _insightApi.Verify(x => x.PutCommand($"sites/{msg.SiteId}/insights/{msg.InsightId}",
                It.Is<UpdateInsightStatusRequest>(x =>
                    x.LastStatus == msg.LastStatus && x.UpdatedByUserId == msg.UpdatedByUserId),
                null), Times.Once);
        }

        [Fact]
        public async Task TicketFunction_RunUpdateInsightNewStatus_WithSourceId_success()
        {
            var msg = new InsightStatusMessage
            {
                SiteId = Guid.NewGuid(),
                InsightId = Guid.NewGuid(),
                LastStatus = InsightStatus.Resolved,
                SourceId = Guid.NewGuid()
            };

            await _function.RunUpdateInsightStatus(JsonConvert.SerializeObject(msg), _context.Object);

            Assert.Equal("TicketUpdateInsightStatus", _logger.Entries[0].Properties["FunctionName"]);

            _insightApi.Verify(x => x.PutCommand($"sites/{msg.SiteId}/insights/{msg.InsightId}",
                It.Is<UpdateInsightStatusRequest>(x =>
                    x.LastStatus == msg.LastStatus && x.SourceId == msg.SourceId),
                null), Times.Once);
        }
        #endregion

        #region RunBroadcastToMarketplace

        [Fact]
        public async Task TicketFunction_RunBroadcastToMarketplace_success()
        {
            var msg = new MarketplaceMessage
            { 
                SiteId = Guid.NewGuid(), 
                TicketId = Guid.NewGuid(), 
                Type = AppMessageType.TicketCreated
            };

            await _function.RunBroadcastToMarketplace(JsonConvert.SerializeObject(msg), _context.Object);

            Assert.Equal("TicketBroadcastToMarketplace", _logger.Entries[0].Properties["FunctionName"]);

            _marketplaceApi.Verify(x => x.PostCommand("messages", 
                It.Is<BroadcastToMarketplaceRequest>(x =>
                    x.SiteId == msg.SiteId
                    && x.Type == msg.Type
                    && x.Payload.TicketId == msg.TicketId), 
                null), Times.Once);
        }

        #endregion
        
        #region RunNotify
        
        [Fact]
        public async Task TicketFunction_RunNotify_customeruser_success()
        {
            var ticketId = Guid.NewGuid();  
            var assigneeId = Guid.NewGuid();  

            _workflowApi.Setup( w=> w.Get<Ticket>( It.IsAny<string>(), null )).ReturnsAsync(new Ticket
            { 
                CustomerId   = _customerId,
                SiteId       = _siteId,
                Id           = ticketId,
                AssigneeType = AssigneeType.CustomerUser,
                AssigneeId   = assigneeId
            });

            var msg = new NotifyMessage
            { 
                CorrelationId =_correlationId,
                CustomerId    =_customerId,
                SiteId        = _siteId, 
                TicketId      = ticketId, 
                OnCreate      = true
            };

            var recipients = await _function.RunNotify(JsonConvert.SerializeObject(msg), _context.Object);

            Assert.NotNull(recipients);
            Assert.Single(recipients);
        }

        [Theory]
        [InlineData(AssigneeType.NoAssignee, "TicketCreated",    true, false)]
        [InlineData(AssigneeType.WorkGroup,  "TicketAssigned",   true, false)]
        [InlineData(AssigneeType.NoAssignee, "TicketReassigned", false, true)]
        [InlineData(AssigneeType.NoAssignee, "TicketUpdated",    false, false)]
        [InlineData(AssigneeType.WorkGroup,  "TicketReassigned", false, true)]
        [InlineData(AssigneeType.WorkGroup,  "TicketUpdated",    false, false)]
        public async Task TicketFunction_RunNotify_success(AssigneeType assigneeType, string expectedTemplateName, bool created, bool reassigned)
        {
            var ticketId = Guid.NewGuid();  
            var assigneeId = Guid.NewGuid();  
            var userId1 = Guid.NewGuid();  
            var userId2 = Guid.NewGuid();  
            var userId3 = Guid.NewGuid();  

            _workflowApi.Setup( w=> w.Get<Ticket>( $"sites/{_siteId}/tickets/{ticketId}", null )).ReturnsAsync(new Ticket
            { 
               CustomerId      = _customerId,
                SiteId         = _siteId,
                Id             = ticketId,
                AssigneeType   = assigneeType,
                AssigneeId     = assigneeType == AssigneeType.NoAssignee ? null : assigneeId,
                Summary        = "Bob's Your Uncle",
                SequenceNumber = "1234"
            });

            if(assigneeType == AssigneeType.WorkGroup)
            { 
                _workflowApi.Setup( w=> w.Get<Workgroup>( $"sites/{_siteId}/workgroups/{assigneeId}", null )).ReturnsAsync(new Workgroup
                { 
                    SiteId       = _siteId,
                    Id           = assigneeId,
                    MemberIds    = new List<Guid> { userId1, userId2, userId3}
                });
            }
            else if(assigneeType == AssigneeType.NoAssignee)
            { 
                _workflowApi.Setup( w=> w.Get<List<NotificationReceiver>>($"sites/{_siteId}/notificationReceivers", null )).ReturnsAsync(new List<NotificationReceiver>
                { 
                    new NotificationReceiver { UserId = userId1 },
                    new NotificationReceiver { UserId = userId2 },
                    new NotificationReceiver { UserId = userId3 }
                });
            }

            var msg = new NotifyMessage
            { 
                CorrelationId =_correlationId,
                CustomerId    =_customerId,
                SiteId        = _siteId, 
                SiteName      = "bob",
                TicketId      = ticketId, 
                OnCreate      = created,
                Reassigned   = reassigned,
                TicketUrl     = "http://bob"
            };

            var recipients = await _function.RunNotify(JsonConvert.SerializeObject(msg), _context.Object);

            Assert.NotNull(recipients);
            Assert.Equal(3, recipients.Length);
            Assert.Equal(userId1, recipients[0].RecipientId);
            Assert.Equal(userId2, recipients[1].RecipientId);
            Assert.Equal(userId3, recipients[2].RecipientId);

            Assert.Equal(created,              recipients[0].OnCreate);
            Assert.Equal(_correlationId,       recipients[0].CorrelationId);
            Assert.Equal(_customerId,          recipients[0].CustomerId);
            Assert.Equal(_siteId,              recipients[0].SiteId);
            Assert.Equal("bob",                recipients[0].SiteName);
            Assert.Equal(ticketId,             recipients[0].TicketId);
            Assert.Equal("http://bob",         recipients[0].TicketUrl);
            Assert.Equal("Bob's Your Uncle",   recipients[0].TicketSummary);
            Assert.Equal("1234",               recipients[0].SequenceNumber);
            Assert.Equal(expectedTemplateName, recipients[0].TemplateName);
        }

        #endregion
        
        #region RunNotifyRecipient
        
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TicketFunction_RunNotifyRecipient_success(bool mobile)
        {
            var ticketId = Guid.NewGuid();  
            var assigneeId = Guid.NewGuid();  

            _directoryApi.Setup( d=> d.Get<CustomerUserPreferences>($"customers/{_customerId}/users/{assigneeId}/preferences", null)).ReturnsAsync(new CustomerUserPreferences
            {
                MobileNotificationEnabled = mobile
            });

            var msg = new NotifyRecipientMessage
            { 
                CorrelationId  = _correlationId,
                CustomerId     = _customerId,
                SiteId         = _siteId, 
                SiteName       = "bob", 
                TicketId       = ticketId, 
                OnCreate       = true,
                RecipientId    = assigneeId,
                TicketSummary  = "Bob's Your Uncle",
                TicketUrl      = "http://bob",
                SequenceNumber = "1234",
                TemplateName   = "TicketCreated"
            };

            var notifications = await _function.RunNotifyRecipient(JsonConvert.SerializeObject(msg), _context.Object);

            Assert.NotNull(notifications);
            Assert.Equal(mobile ? 2 : 1, notifications?.Length);

            Assert.Equal(_correlationId,     notifications[0].CorrelationId);
            Assert.Equal(_customerId,        notifications[0].CustomerId);
            Assert.Equal(assigneeId,         notifications[0].UserId);
            Assert.Equal("email",            notifications[0].CommunicationType);
            Assert.Equal("TicketCreated",    notifications[0].TemplateName);
            Assert.True(string.IsNullOrWhiteSpace(notifications[0].UserType));
            Assert.True(string.IsNullOrWhiteSpace(notifications[0].Locale));

            Assert.Equal("1234",             notifications[0].Data["TicketSequenceNumber"]);
            Assert.Equal("Bob's Your Uncle", notifications[0].Data["TicketSummary"]);
            Assert.Equal("bob",              notifications[0].Data["SiteName"]);
            Assert.Equal("http://bob",       notifications[0].Data["TicketUrl"]);

            if(mobile)
            {
                Assert.Equal(_correlationId,     notifications[1].CorrelationId);
                Assert.Equal(_customerId,        notifications[1].CustomerId);
                Assert.Equal(assigneeId,         notifications[1].UserId);
                Assert.Equal("pushnotification", notifications[1].CommunicationType);
                Assert.Equal("TicketCreated",    notifications[1].TemplateName);
                Assert.True(string.IsNullOrWhiteSpace(notifications[1].UserType));
                Assert.True(string.IsNullOrWhiteSpace(notifications[1].Locale));

                Assert.Equal("1234",             notifications[1].Data["TicketSequenceNumber"]);
                Assert.Equal("Bob's Your Uncle", notifications[1].Data["TicketSummary"]);
                Assert.Equal("bob",              notifications[1].Data["SiteName"]);
                Assert.Equal("http://bob",       notifications[1].Data["TicketUrl"]);
            }

        }

        #endregion      
    }
}