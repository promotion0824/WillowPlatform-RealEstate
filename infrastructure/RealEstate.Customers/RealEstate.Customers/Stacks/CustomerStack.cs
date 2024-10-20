using Pulumi;
using RealEstate.Customers.Components.ADX;
using RealEstate.Customers.Components.DigitalTwins;
using RealEstate.Customers.Contexts;
using System.Linq;
using RealEstate.Customers.Components.Security;

namespace RealEstate.Customers.Stacks;

public class CustomerStack : Stack
{
    public CustomerStack()
    {
        var context = new EnvContext();
        
        foreach (var customer in context.Customers())
        {
            _ = new DigitalTwin(
                new DigitalTwinArgs(
                    customer.Adt.Name,
                    customer.Adt.ResourceGroupName ?? customer.ResourceGroupName,
                    customer.CustomerTags)
                {
                    SubscriptionId = customer.Adt.SubscriptionId,
                    DigitalTwinCore = context.DigitalTwinCore,
                    IdentityArgs = customer.Adt.Identities?.Select(x => new IdentityArgs(x.PrincipalId, x.TenantId, x.Name, x.Role, customer.Adt.SubscriptionId, x.PrincipalType))
                });
            
            if (string.IsNullOrWhiteSpace(customer.Adx.Database))
            {
                continue;
            }

            _ = new AdxDatabase(
                new AdxDatabaseArgs(
                    customer.Adx.Database,
                    customer.Adx.ResourceGroup,
                    customer.CustomerTags,
                    customer.Adx.Location,
                    customer.Adx.Cluster,
                    customer.Adx.SubscriptionId)
                {
                    DigitalTwinCore = context.DigitalTwinCore,
                    OldPrincipalName = customer.Adx.OldPrincipalName,
                    IdentityArgs = customer.Adx.Identities?.Select(x => new IdentityArgs(x.PrincipalId, x.TenantId, x.Name, x.Role, customer.Adx.SubscriptionId, x.PrincipalType))
                });
        }
    }
}