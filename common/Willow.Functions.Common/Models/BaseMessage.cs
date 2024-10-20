using System;

namespace Willow.Functions.Common
{
    public class BaseMessage
    {
        public Guid CorrelationId { get; init; } = Guid.NewGuid();
    }

    public class StartMessage : BaseMessage
    {
    }

    public class CustomerMessage : BaseMessage
    {
        public Guid CustomerId { get; init; }
    }

    public class SiteMessage : CustomerMessage
    {
        public Guid SiteId { get; init; }
    }   
}
