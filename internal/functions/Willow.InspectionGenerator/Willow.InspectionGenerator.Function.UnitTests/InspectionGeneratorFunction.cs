using AutoFixture;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Willow.Api.Client;
using Willow.Common;
using Willow.Functions.Common;
using Willow.InspectionGenerator.Function;
using Willow.InspectionGenerator.Function.Models;
using Willow.Platform.Models;
using Xunit;

namespace Willow.Inspections.Function.UnitTests
{
    public class InspectionGeneratorFunctionTests
    {
        public Fixture Fixture = new Fixture();

        private readonly InspectionGeneratorFunction _function;
        private readonly Mock<IRestApi> _directoryCore;
        private readonly Mock<IRestApi> _workflowCore;
        private readonly Mock<FunctionContext> _context;
        private readonly Mock<IDateTimeService> _datetimeService;

        public InspectionGeneratorFunctionTests()
        {
            _directoryCore = new Mock<IRestApi>();
            _workflowCore = new Mock<IRestApi>();
            _context = new Mock<FunctionContext>();
            _datetimeService = new Mock<IDateTimeService>();

            var svc = new InspectionGeneratorService(_datetimeService.Object, _directoryCore.Object, _workflowCore.Object);

            _function = new InspectionGeneratorFunction(svc);
        }

        [Fact]
        public async Task InspectionGeneratorFunction_RunStart_success()
        {
            var msg = new StartMessage
            {
                CorrelationId = Guid.NewGuid()
            };

            var customers = Fixture.Build<Customer>().CreateMany(3).ToList();

            _directoryCore.Setup(d => d.Get<List<Customer>>("customers?active=true", It.IsAny<object>()))
                .ReturnsAsync(customers);

            var result = await _function.RunStart(JsonConvert.SerializeObject(msg), _context.Object);

            Assert.NotNull(result);
            Assert.Equal(3, result.Length);
            Assert.True(result.All(x => x.CorrelationId == msg.CorrelationId));

            result.Select(x => x.CustomerId).Should().BeEquivalentTo(customers.Select(x => x.Id));
        }

        [Fact]
        public async Task InspectionGeneratorFunction_RunCustomer_success()
        {
            var msg = new CustomerMessage
            {
                CorrelationId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid()
            };

            var sites = Fixture.Build<Site>().CreateMany(3).ToList();

            _directoryCore.Setup(d => d.Get<List<Site>>($"sites/customer/{msg.CustomerId}?isInspectionEnabled=true", It.IsAny<object>()))
                .ReturnsAsync(sites);

            var result = await _function.RunCustomer(JsonConvert.SerializeObject(msg), _context.Object);

            Assert.NotNull(result);
            Assert.Equal(3, result.Length);
            Assert.True(result.All(x => x.CustomerId == msg.CustomerId));
            Assert.True(result.All(x => x.CorrelationId == msg.CorrelationId));

            result.Select(x => x.SiteId).Should().BeEquivalentTo(sites.Select(x => x.Id));
        }

        [Fact]
        public async Task InspectionGeneratorFunction_RunSite_success()
        {
            var msg = new SiteMessage
            {
                CorrelationId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                SiteId = Guid.NewGuid()
            };

            var scheduledInspections = Fixture.Build<GenerateInspectionRecord>().CreateMany(3).ToList();

            _workflowCore.Setup(x => x.Get<List<GenerateInspectionRecord>>($"scheduledinspections/site/{msg.SiteId}", It.IsAny<object>()))
                .ReturnsAsync(scheduledInspections);

            var result = await _function.RunSite(JsonConvert.SerializeObject(msg), _context.Object);

            Assert.NotNull(result);
            Assert.Equal(3, result.Length);
            Assert.True(result.All(x => x.SiteId == msg.SiteId));
            Assert.True(result.All(x => x.CustomerId == msg.CustomerId));
            Assert.True(result.All(x => x.CorrelationId == msg.CorrelationId));

            result.Select(x => x.InspectionId).Should().BeEquivalentTo(scheduledInspections.Select(x => x.Id));
            result.Select(x => x.SiteNow).Should().BeEquivalentTo(scheduledInspections.Select(x => x.SiteNow));
        }

        [Fact]
        public async Task InspectionGeneratorFunction_RunRecordCreator_success()
        {
            var msg = new GenerateInspectionRecordMessage
            {
                CorrelationId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                SiteId = Guid.NewGuid(),
                InspectionId = Guid.NewGuid(), 
                SiteNow = DateTime.UtcNow
            };

            var inspectionRecord = Fixture.Build<InspectionRecord>()
                .With(x => x.InspectionId, msg.InspectionId)
                .Create();

            _workflowCore.Setup(x => x.Post<GenerateInspectionRecordRequest, InspectionRecord>("scheduledinspection/generate", 
                It.IsAny<GenerateInspectionRecordRequest>(), 
                null))
                .ReturnsAsync(inspectionRecord);

            var utcNow = DateTime.UtcNow;
            var effectiveDate = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, 0, 0);
            _datetimeService.Setup(w => w.UtcNow).Returns(utcNow);

            var result = await _function.RunCreateRecord(JsonConvert.SerializeObject(msg), _context.Object);

            Assert.NotNull(result);
            Assert.Equal(msg.SiteNow, result.SiteNow);
            Assert.Equal(effectiveDate, result.EffectiveDate);
            Assert.Equal(inspectionRecord.Id, result.InspectionRecordId);
            Assert.Equal(msg.InspectionId, result.InspectionId);
            Assert.Equal(msg.SiteId, result.SiteId);
            Assert.Equal(msg.CustomerId, result.CustomerId);
            Assert.Equal(msg.CorrelationId, result.CorrelationId);
        }

        [Fact]
        public async Task InspectionGeneratorFunction_RunChecks_success()
        {
            var utcNow = DateTime.UtcNow;
            var effectiveDate = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, 0, 0);

            var msg = new InspectionRecordMessage
            {
                CorrelationId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                SiteId = Guid.NewGuid(),
                EffectiveDate = effectiveDate,
                InspectionId = Guid.NewGuid(),
                InspectionRecordId = Guid.NewGuid(),
                SiteNow = effectiveDate
            };

            var inspection = Fixture.Build<Inspection>()
                .Create();

            _workflowCore.Setup(x => x.Get<Inspection>($"sites/{msg.SiteId}/inspections/{msg.InspectionId}", It.IsAny<object>()))
                .ReturnsAsync(inspection);

            var result = await _function.RunChecks(JsonConvert.SerializeObject(msg), _context.Object);

            Assert.NotNull(result);
            Assert.Equal(inspection.Checks.Count(), result.Length);

            Assert.True(result.All(x => x.SiteNow == msg.SiteNow));
            Assert.True(result.All(x => x.EffectiveDate == msg.EffectiveDate));
            Assert.True(result.All(x => x.InspectionRecordId == msg.InspectionRecordId));
            Assert.True(result.All(x => x.InspectionId == msg.InspectionId));
            Assert.True(result.All(x => x.SiteId == msg.SiteId));
            Assert.True(result.All(x => x.CustomerId == msg.CustomerId));
            Assert.True(result.All(x => x.CorrelationId == msg.CorrelationId));

            result.Select(x => x.CheckId).Should().BeEquivalentTo(inspection.Checks.Select(x => x.Id));
        }

        [Fact]
        public async Task InspectionGeneratorFunction_RunCheckRecordCreator_success()
        {
            var utcNow = DateTime.UtcNow;
            var effectiveDate = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, 0, 0);

            var msg = new GenerateCheckRecordMessage
            {
                CorrelationId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                SiteId = Guid.NewGuid(),
                EffectiveDate = effectiveDate,
                InspectionId = Guid.NewGuid(),
                InspectionRecordId = Guid.NewGuid(),
                SiteNow = effectiveDate, 
                CheckId = Guid.NewGuid()
            };

            await _function.RunCreateCheck(JsonConvert.SerializeObject(msg), _context.Object);

            _workflowCore.Verify( w => w.Post<GenerateCheckRecordRequest, GenerateCheckRecord>("scheduledinspection/generate/check", 
                It.Is<GenerateCheckRecordRequest>(x => 
                    x.InspectionId == msg.InspectionId
                    && x.InspectionRecordId == msg.InspectionRecordId
                    && x.CheckId == msg.CheckId 
                    && x.SiteId == msg.SiteId 
                    && x.EffectiveDate == effectiveDate), 
                null), Times.Once);
        }
    }
}