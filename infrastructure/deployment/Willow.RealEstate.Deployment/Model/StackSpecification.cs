using System.Collections.Generic;

namespace Willow.RealEstate.Deployment
{
    internal class StackSpecification
    {
        internal string EnvironmentCode { get; set; }
        internal string RegionCode { get; set; }
        internal string RegionName { get; set; }

        internal IDictionary<string,ExistingResourceSpecification> ExistingApps { get; set; }
        internal IList<ResourceGroupSpecification>                 ResourceGroups { get; set; }
    }
}
