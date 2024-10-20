using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Pulumi;
using Pulumi.AzureNative.Authorization;
using Pulumi.AzureNative.Web;
using RealEstate.Customers.Components.ADX;
using RealEstate.Customers.Components.Tags;
using RealEstate.Customers.Helpers;
using RealEstate.Customers.Stacks;

namespace RealEstate.Customers.Components.DigitalTwins
{
    public class DigitalTwin : ComponentResource
    {
        [Output("createdTime")] public Output<string> CreatedTime { get; private set; }

        [Output("hostName")] public Output<string> HostName { get; private set;  }

        [Output("identity")] public Output<Pulumi.AzureNative.DigitalTwins.Outputs.DigitalTwinsIdentityResponse> Identity { get; private set; }

        [Output("lastUpdatedTime")] public Output<string> LastUpdatedTime { get; private set; }

        [Output("location")] public Output<string> Location { get; private set; }

        [Output("name")] public Output<string> Name { get; private set; }

        [Output("privateEndpointConnections")]
        public Output<ImmutableArray<Pulumi.AzureNative.DigitalTwins.Outputs.PrivateEndpointConnectionResponse>> PrivateEndpointConnections { get; private set; }

        [Output("provisioningState")] public Output<string> ProvisioningState { get; private set; }

        [Output("publicNetworkAccess")] public Output<string> PublicNetworkAccess { get; private set; }

        [Output("tags")] public Output<ImmutableDictionary<string, string>> Tags { get; private set; }

        [Output("type")] public Output<string> Type { get; private set; }

        public DigitalTwin(DigitalTwinArgs args) : base($"{StringConstants.ComponentNamespace}:component:DigitalTwin", args.Name)
        {
            var digitalTwin = new Pulumi.AzureNative.DigitalTwins.DigitalTwin(args.Name,
                new Pulumi.AzureNative.DigitalTwins.DigitalTwinArgs
                {
                    ResourceGroupName = args.ResourceGroup,
                    ResourceName = args.Name,
                    PublicNetworkAccess = "Enabled",
                    Tags = args.Tags
                },
                new CustomResourceOptions
                {
                    Protect = true,
                    Parent = this,
                    IgnoreChanges = TagConstants.TagChangesToIgnore.ToList()
                });

            _ = GetWebApp.Invoke(new GetWebAppInvokeArgs
            {
                Name = args.DigitalTwinCore.Name,
                ResourceGroupName = args.DigitalTwinCore.ResourceGroup
            }).Apply(result =>
            {
                if (result.Identity == null)
                {
                    return null;
                }

                var resourceName = $"{args.Name}/{result.Name}";
                
                return new RoleAssignment(resourceName,
                    new RoleAssignmentArgs
                    {
                        PrincipalId = result.Identity.PrincipalId,
                        Scope = digitalTwin.Id,
                        RoleDefinitionId = GetRoleDefinition("owner", args.SubscriptionId),
                        RoleAssignmentName = Guid.NewGuid().ToString(),
                        PrincipalType = "ServicePrincipal",
                    },
                    new CustomResourceOptions
                    {
                        Parent = this,
                        IgnoreChanges = {"roleAssignmentName"},
                        DeleteBeforeReplace = true 
                    });
            });

            if (args.IdentityArgs != null)
            {
                foreach (var identity in args.IdentityArgs)
                {
                    var name = $"{args.Name}/{identity.Name}";
                    var roleAssignmentArgs = new RoleAssignmentArgs
                    {
                        PrincipalId = identity.PrincipalId,
                        Scope = digitalTwin.Id,
                        RoleDefinitionId = GetRoleDefinition(identity.Role, identity.SubscriptionId),
                        RoleAssignmentName = Guid.NewGuid().ToString(),
                        PrincipalType = "ServicePrincipal"
                    };

                    var assignment = new RoleAssignment(name, roleAssignmentArgs, new CustomResourceOptions
                    {
                        Parent = this,
                        IgnoreChanges = {"roleAssignmentName"},
                        DeleteBeforeReplace = true 
                    });
                }
            }

            CreatedTime = digitalTwin.CreatedTime;
            HostName = digitalTwin.HostName;
            Identity = digitalTwin.Identity;
            LastUpdatedTime = digitalTwin.LastUpdatedTime;
            Location = digitalTwin.Location;
            Name = digitalTwin.Name;
            PrivateEndpointConnections = digitalTwin.PrivateEndpointConnections;
            ProvisioningState = digitalTwin.ProvisioningState;
            PublicNetworkAccess = digitalTwin.PublicNetworkAccess;
            Tags = digitalTwin.Tags;
            Type = digitalTwin.Type;
        }

        private string GetRoleDefinition(string role, string subscriptionId)
        {
            var format = $"/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/{{0}}";

            if (role.Equals("owner", System.StringComparison.InvariantCultureIgnoreCase))
                return string.Format(format, "bcd981a7-7f74-457b-83e1-cceb9e632ffe");

            if (role.Equals("reader", System.StringComparison.InvariantCultureIgnoreCase))
                return string.Format(format, "d57506d4-4c8d-48b1-8587-93c323f6a5a3");

            throw new System.Exception("Invalid Digital Twin role");
        }
    }
}
