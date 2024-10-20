using Pulumi;

namespace RealEstate.Customers.Components;

public class BaseArgs
{
    protected BaseArgs(string name, Input<string> resourceGroup, InputMap<string> tags)
    {
        Name = name;
        ResourceGroup = resourceGroup;
        Tags = tags;
    }

    public string Name { get; }
    public Input<string> ResourceGroup { get; }
    public InputMap<string> Tags { get; }
    public int LogRetention { get; } = 90;
}