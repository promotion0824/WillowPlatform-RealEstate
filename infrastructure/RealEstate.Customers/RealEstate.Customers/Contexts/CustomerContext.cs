using Pulumi;
using RealEstate.Customers.Components.Tags;
using RealEstate.Customers.Stacks;
using System.Collections.Generic;
using System.Linq;

namespace RealEstate.Customers.Contexts;

public class CustomerContext
{
    private readonly Customer _customer;
    private readonly EnvContext _envContext;

    public CustomerContext(Customer customer, EnvContext envContext)
    {
        _customer = customer;
        _envContext = envContext;

        Adt = new AdtContext(ResourceName, _customer, envContext);

        if (_customer.Adx != null)
        {
            Adx = new AdxContext(_customer, _envContext);
        }
    }

    public string Region => _envContext.Region;

    public string Code => _customer.Code;

    public string Environment => _envContext.Environment;

    public string Tier => _envContext.Tier;
    
    public InputMap<string> CustomerTags => new TagBuilder("Digital Twin Core", _customer.Name, Environment).Build();

    private string ResourceName => $"wil-{Environment}-lda-{Code}-{Region}";

    public string ResourceGroupName => $"t{Tier}-{ResourceName}-{_customer.ResourceGroupSuffix}";
    
    public AdxContext Adx { get; }

    public AdtContext Adt { get; }
}

public class AdtContext
{
    private readonly string _resourceName;
    private readonly Customer _customer;
    private readonly EnvContext _envContext;

    public AdtContext(string resourceName, Customer customer, EnvContext envContext)
    {
        _resourceName = resourceName;
        _customer = customer;
        Identities = customer.Adt.Identities?.Select(x => new IdentityContext(x, envContext, customer));
        _envContext = envContext;
    }
    
    public string Name => $"{_resourceName}-{_customer.Adt.Name}";
    public IEnumerable<IdentityContext> Identities { get; private set; }
    public string SubscriptionId => _envContext.Adt?.SubscriptionId;
    public string? ResourceGroupName => _customer.Adt.ResourceGroupName;
}

public class IdentityContext
{
    private readonly Identity _identity;
    private readonly EnvContext _envContext;
    private readonly Customer _customer;

    public IdentityContext(Identity identity, EnvContext envContext, Customer customer)
    {
        _identity = identity;
        _customer = customer;
        _envContext = envContext;
    }

    public string? PrincipalId => _identity.PrincipalId;
    public string? TenantId => _identity.TenantId;
    public string? Name => _identity.Name;
    public string? Role => _identity.Role;
    public string? PrincipalType => _identity.PrincipalType;
}

public class AdxContext
{
    private readonly Customer _customer;
    private readonly EnvContext _envContext;

    public AdxContext(Customer customer, EnvContext envContext)
    {
        _customer = customer;
        _envContext = envContext;
        Identities = customer.Adx.Identities?.Select(x => new IdentityContext(x, envContext, customer));
    }
    
    public string Database => _customer.Adx.Database;

    public string Cluster => _envContext.Adx.Cluster;

    public string Location => _envContext.Adx.Location;

    public string ResourceGroup => _envContext.Adx.ResourceGroup;

    public string SubscriptionId => _envContext.Adx.SubscriptionId;

    public string OldPrincipalName => _customer.Adx.PrincipalName;

    public IEnumerable<IdentityContext> Identities { get; private set; }
}