using Pulumi;
using RealEstate.Customers.Settings;
using System.Collections.Generic;
using RealEstate.Customers.Components.Security;

namespace RealEstate.Customers.Components.ADX;

public class AdxDatabaseArgs: BaseArgs
{
    public string ClusterName { get; }
    public string DatabaseName { get; }
    public string Location { get; }
    public string SubscriptionId { get; }
    public string OldPrincipalName { get; set; }
    public string HotCachePeriod { get; }
    public WebAppSettings DigitalTwinCore { get; set; }
    public IEnumerable<IdentityArgs> IdentityArgs { get; set; }

    public AdxDatabaseArgs(string name, Input<string> resourceGroup, InputMap<string> tags, string location, string clusterName, string subscriptionId, string hotCachePeriod = null)
        : base($"{clusterName}/{name}", resourceGroup, tags)
    {
        DatabaseName = name;
        Location = location;
        ClusterName = clusterName;
        SubscriptionId = subscriptionId;
        HotCachePeriod = hotCachePeriod ?? "P90D";
    }
}