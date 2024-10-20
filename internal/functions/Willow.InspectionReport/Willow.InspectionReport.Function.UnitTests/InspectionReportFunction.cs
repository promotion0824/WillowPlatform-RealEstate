using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Azure.Functions.Worker;

using Xunit;
using Moq;

using Newtonsoft.Json;

using Willow.Api.Client;
using Willow.Platform.Models;

namespace Willow.InspectionReport.Function.UnitTests
{
    public class InspectionReportFunctionTests
    {
        private readonly InspectionReportFunction _function;
        private readonly Mock<IRestApi> _directoryCore;
        private readonly Mock<IRestApi> _workflowCore;
        private readonly Mock<FunctionContext> _context;
        private readonly Guid _customerId1 = Guid.NewGuid();
        private readonly Guid _customerId2 = Guid.NewGuid();
        private readonly Guid _customerId3 = Guid.NewGuid();

        public InspectionReportFunctionTests()
        {
            _directoryCore = new Mock<IRestApi>();
            _workflowCore = new Mock<IRestApi>();
            _context = new Mock<FunctionContext>();

            var svc = new InspectionReportService(_directoryCore.Object, _workflowCore.Object);

            _function = new InspectionReportFunction(svc);
        }

        [Fact]
        public async Task InspectionReportFunction_RunStart_success()
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
        public async Task InspectionReportFunction_RunCustomer_success()
        {
            var correlationId = Guid.NewGuid();
            var siteId1 = Guid.NewGuid();
            var siteId2 = Guid.NewGuid();
            var siteId3 = Guid.NewGuid();

            _directoryCore.Setup( d=> d.Get<List<Site>>($"sites/customer/{_customerId1}?isInspectionEnabled=true", It.IsAny<object>())).ReturnsAsync(new List<Site>
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
        public async Task InspectionReportFunction_RunSite_success()
        {
            var correlationId = Guid.NewGuid();
            var siteId1 = Guid.NewGuid();
            var msg = new {CorrelationId = correlationId, CustomerId = _customerId1, SiteId = siteId1 };
            
            await _function.RunSite(JsonConvert.SerializeObject(msg), _context.Object);

            _workflowCore.Verify( w=> w.Post<string>($"inspections/reports/site/{siteId1}"), Times.Once);
        }
    }
}