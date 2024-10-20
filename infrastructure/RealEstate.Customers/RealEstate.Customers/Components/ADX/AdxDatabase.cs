using System.Collections.Generic;
using Pulumi;
using Pulumi.AzureNative.Kusto;
using Pulumi.AzureNative.Web;
using RealEstate.Customers.Helpers;
using PrincipalType = Pulumi.AzureNative.Kusto.PrincipalType;

namespace RealEstate.Customers.Components.ADX;

public class AdxDatabase : ComponentResource
{
    [Output("hotCachePeriod")] public Output<string> HotCachePeriod { get; private set; }

    [Output("isFollowed")] public Output<bool> IsFollowed { get; private set; }

    [Output("kind")] public Output<string> Kind { get; private set; }

    [Output("location")] public Output<string> Location { get; private set; }

    [Output("name")] public Output<string> Name { get; private set; }

    [Output("provisioningState")] public Output<string> ProvisioningState { get; private set; }

    [Output("softDeletePeriod")] public Output<string> SoftDeletePeriod { get; private set; }

    [Output("type")] public Output<string> Type { get; private set; }

    public AdxDatabase(AdxDatabaseArgs args) : base($"{StringConstants.ComponentNamespace}:component:AdxDatabase", args.Name)
    {
        var adxProvider = new Pulumi.AzureNative.Provider($"{args.ClusterName}/{args.DatabaseName}/provider", new Pulumi.AzureNative.ProviderArgs
        {
            SubscriptionId = args.SubscriptionId
        });

        var adxDatabase = new ReadWriteDatabase(args.Name,
            new ReadWriteDatabaseArgs
            {
                ResourceGroupName = args.ResourceGroup,
                ClusterName = args.ClusterName,
                DatabaseName = args.DatabaseName,
                Location = args.Location,
                HotCachePeriod = "P90D"
            },
            new CustomResourceOptions
            {
                Protect = true,
                Parent = this,
                Provider = adxProvider
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
            
            var resourceName = $"{args.ClusterName}/{args.DatabaseName}/{result.Name}";
            var aliases = new List<Input<Alias>>();
            if (!string.IsNullOrWhiteSpace(args.OldPrincipalName))
            {
                aliases.Add(new Alias { Urn = $"urn:pulumi:{Deployment.Instance.StackName}::RealEstate.Customers::azure-native:kusto:DatabasePrincipalAssignment::{args.ClusterName}/{args.DatabaseName}/{args.OldPrincipalName}" });
            }

            return new DatabasePrincipalAssignment(resourceName,
                new DatabasePrincipalAssignmentArgs
                {
                    ClusterName = args.ClusterName,
                    DatabaseName = args.DatabaseName,
                    ResourceGroupName = args.ResourceGroup,
                    Role = DatabasePrincipalRole.Admin.ToString(),
                    PrincipalType = PrincipalType.App,
                    PrincipalAssignmentName = result.Name,
                    PrincipalId = result.Identity.PrincipalId,
                    TenantId = result.Identity.TenantId
                },
                new CustomResourceOptions
                {
                    Protect = true,
                    Parent = this,
                    Provider = adxProvider,
                    Aliases = aliases,
                    DeleteBeforeReplace = true,
                    DependsOn = adxDatabase
                });
        });

        if (args.IdentityArgs != null)
        {            
            var assignments = new List<DatabasePrincipalAssignment>();
            foreach (var identity in args.IdentityArgs)
                assignments.Add(new DatabasePrincipalAssignment($"{args.ClusterName}/{args.DatabaseName}/{identity.Name}",
                new DatabasePrincipalAssignmentArgs
                {
                    ClusterName = args.ClusterName,
                    DatabaseName = args.DatabaseName,
                    ResourceGroupName = args.ResourceGroup,
                    Role = identity.Role,
                    PrincipalType = identity.PrincipalType ?? PrincipalType.App.ToString(),
                    PrincipalAssignmentName = identity.Name,
                    PrincipalId = identity.PrincipalId,
                    TenantId = identity.TenantId
                },
                new CustomResourceOptions
                {
                    Parent = this,
                    Provider = adxProvider,
                    DeleteBeforeReplace = true
                }));

        }

        HotCachePeriod = adxDatabase.HotCachePeriod;
        Type = adxDatabase.Type;
        Name = adxDatabase.Name;
        IsFollowed = adxDatabase.IsFollowed;
        Kind = adxDatabase.Kind;
        Location = adxDatabase.Location;
        ProvisioningState = adxDatabase.ProvisioningState;
        SoftDeletePeriod = adxDatabase.SoftDeletePeriod;
    }
}