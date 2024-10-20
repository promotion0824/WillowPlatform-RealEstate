using System.Collections.Generic;
using Pulumi;
using Pulumi.Random;

namespace Willow.RealEstate.Deployment
{
    internal class RoleAssignmentSpecification
    {
        internal ExistingResourceSpecification ExistingResource { get; init; }
        internal string                        NewResource      { get; init; }
    }
}
