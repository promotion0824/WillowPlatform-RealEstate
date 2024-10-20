namespace RealEstate.Customers.Components.Security
{
    public class IdentityArgs
    {
        public string PrincipalId { get; }
        public string TenantId { get; }
        public string Name { get; }
        public string Role { get; }
        public string SubscriptionId { get; }
        public string? PrincipalType { get; }

        public IdentityArgs(string principalId, string tenantId, string name, string role, string subscriptionId, string? principalType)
        {
            PrincipalId = principalId;
            TenantId = tenantId;
            Name = name;
            Role = role;
            SubscriptionId = subscriptionId;
            PrincipalType = principalType;
        }
    }
}
