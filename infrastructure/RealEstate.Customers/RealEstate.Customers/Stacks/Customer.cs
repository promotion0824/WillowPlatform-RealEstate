using System.Collections.Generic;

namespace RealEstate.Customers.Stacks;

public class Customer
{
    public string Name { get; set; }
    public string Code { get; set; }
    public string Region { get; set; }
    public string ResourceGroupSuffix { get; set; } = "app-rsg";
    public Adt Adt { get; set; } = new();
    public Adx Adx { get; set; } = new();
}

public class Adx
{
    public string? Id { get; set; } = null;
    public string Database { get; set; }
    public string PrincipalName { get; set; }
    public IEnumerable<Identity> Identities { get; set; }
}

public class Adt
{
    public string? Id { get; set; } = null;
    public string Name { get; set; } = "adt";
    public IEnumerable<Identity> Identities { get; set; }
    public string? ResourceGroupName { get; set; }
}

public class Identity
{
    public string? PrincipalId { get; set; } = null;
    public string? TenantId { get; set; } = null;
    public string? Name { get; set; } = null;
    public string? Role { get; set; } = null;
    public string? PrincipalType { get; set; } = null;
}