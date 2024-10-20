using System.Collections.Generic;

using Pulumi.AzureNative.Web;

namespace Willow.RealEstate.Deployment
{
    internal class AppServicePlanSpecification
    {
        internal string Name        { get; init; }
        internal string Kind        { get; init; } = "app,linux";
        internal string Sku         { get; init; } = "P1v2";
        internal int    MaxScale    { get; init; } = 20;

        internal IList<FunctionAppSpecification> FunctionApps { get; init; }
    }
}
