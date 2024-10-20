using System.Collections.Generic;

using Pulumi.AzureNative.Storage;

namespace Willow.RealEstate.Deployment
{
    internal class StorageAccountSpecification
    {
        internal string  Name  { get; init; }
        internal Kind    Kind  { get; init; } = Kind.StorageV2;
        internal SkuName Sku   { get; init; } = SkuName.Standard_LRS;
        internal bool    GiveServiceConnectionAccess   { get; init; } = false;

        internal IEnumerable<RoleAssignmentSpecification>   RoleAssignments { get; init; }
        internal IEnumerable<StorageContainerSpecification> Containers      { get; init; }
    }
}
