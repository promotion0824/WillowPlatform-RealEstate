using System.Collections.Generic;

namespace Willow.RealEstate.Deployment
{
    internal class ResourceGroupSpecification
    {
        internal string Name { get; init; }

        internal IList<AppServicePlanSpecification> AppServicePlans         { get; init; }
        internal IList<StorageAccountSpecification> StorageAccounts         { get; init; }
        internal IList<StorageAccountSpecification> ExistingStorageAccounts { get; init; }
        internal IList<ServiceBusSpecification>     ServiceBusNamespaces    { get; init; }
    }
}
