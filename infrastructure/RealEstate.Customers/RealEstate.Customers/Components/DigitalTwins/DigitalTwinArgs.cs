using Pulumi;
using System.Collections.Generic;
using RealEstate.Customers.Components.Security;
using RealEstate.Customers.Settings;

namespace RealEstate.Customers.Components.DigitalTwins;

public class DigitalTwinArgs : BaseArgs
{
    public IEnumerable<IdentityArgs> IdentityArgs { get; set; }
    public WebAppSettings DigitalTwinCore { get; set; }
    public string SubscriptionId { get; set; }

    public DigitalTwinArgs(string name, Input<string> resourceGroup, InputMap<string> tags)
        : base(name, resourceGroup, tags)
    {
    }
}