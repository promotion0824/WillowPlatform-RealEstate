using System.Collections.Generic;

using Pulumi.AzureNative.Storage;

namespace Willow.RealEstate.Deployment
{
    internal class ServiceBusSpecification
    {
        internal string  Name  { get; init; }
        internal string  Sku   { get; init; } = "Standard";

        internal IEnumerable<ServiceBusRoleAssignment>   RoleAssignments { get; init; }
        internal IEnumerable<string> Queues { get; init; }
    }

    internal class ServiceBusRoleAssignment : RoleAssignmentSpecification
    {
        internal bool Sender    { get; init; } = false;
        internal bool Listener  { get; init; } = false;
    }
}
