using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Pulumi;
using Pulumi.AzureNative.Authorization;
using ServiceBus = Pulumi.AzureNative.ServiceBus;
using Pulumi.AzureNative.Insights;
using Pulumi.AzureNative.KeyVault;
using KeyVault = Pulumi.AzureNative.KeyVault;
using Pulumi.AzureNative.KeyVault.Inputs;
using Pulumi.AzureNative.ServiceBus.Inputs;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;
using Pulumi.Random;

namespace Willow.RealEstate.Deployment
{
    public class DeploymentStack : Stack
    {
        private Dictionary<string, Output<string>> _newResourcePrincipalIds = new Dictionary<string, Output<string>>();
        private Dictionary<string, Resource>       _newResources = new Dictionary<string, Resource>();
        private StackSpecification                 _specification;
        private readonly string                    _prefix;
        private readonly string                    _shortPrefix;
        private readonly string                    _newKeyVaultName;
        private readonly Output<string>            _newKeyVaultId;


        public DeploymentStack()
        {
            System.Diagnostics.Debugger.Launch();

            var config = new Config();

            _specification   = RealEstate.BuildStackSpecification(config.Require("environmentCode"), config.Require("regionCode"));
            _prefix          = $"wil-{_specification.EnvironmentCode}-plt-{_specification.RegionCode}";
            _shortPrefix     = $"wil{_specification.EnvironmentCode}plt{_specification.RegionCode}";
            _newKeyVaultName = $"wil-{_specification.EnvironmentCode}-plt-{_specification.RegionCode}-kvl2";

            StorageAccount webjobsStorage = null;

            var newKeyVault = GetVault.Invoke(new GetVaultInvokeArgs
            {
                ResourceGroupName = $"t3-{_prefix}-app-rsg",
                VaultName = _newKeyVaultName
            });

            _newKeyVaultId = newKeyVault.Apply( kv=> kv.Id );

            // Add role assignment for Willow Engineers
            // DeployRoleAssignment("c791ea50-fa7e-4842-b285-ad230e1dea9c", "Engineers_Willow", _newKeyVaultId, Roles.KeyVault.SecretOfficer, PrincipalType.Group);

            foreach (var resourceGroup in _specification.ResourceGroups)
            {
                // Assumes the resource group already exists
                
                // If we're creating any function apps then we need to create a storage account for them
                if(resourceGroup.AppServicePlans?.Where( plan=> plan.FunctionApps?.Any() ?? false )?.Any() ?? false)
                {
                    webjobsStorage = DeployStorageAccount(new StorageAccountSpecification
                    {
                        Name = $"{_shortPrefix}funcsto"
                    },
                    resourceGroup.Name);
                }

                // Deploy App Service Plans
                DeployAppServicePlans(resourceGroup.AppServicePlans, resourceGroup.Name);
                
                // Deploy Azure Storage Accounts
                DeployStorageAccounts(resourceGroup.StorageAccounts, resourceGroup.Name);
                
                // Deploy Service Bus Namespaces
                DeployServiceBusNamespaces(resourceGroup.ServiceBusNamespaces, resourceGroup.Name);

                // Ensure certain app services have access to existing storage accounts
                UpdateExistingStorageAccounts(resourceGroup.ExistingStorageAccounts, resourceGroup.Name);
          }
        }

        #region Private

        private static Output<string> GetStorageAccountConnectionString(Input<string> resourceGroupName, Input<string> accountName)
        {
            // Retrieve the primary storage account key.
            var storageAccountKeys = ListStorageAccountKeys.Invoke(new ListStorageAccountKeysInvokeArgs
            {
                ResourceGroupName = resourceGroupName,
                AccountName = accountName
            });

            return storageAccountKeys.Apply(keys =>
            {
                var primaryStorageKey = keys.Keys[0].Value;

                // Build the connection string to the storage account.
                return Output.Format($"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={primaryStorageKey}");
            });
        }

        private void DeployAppServicePlans(IEnumerable<AppServicePlanSpecification> appServicePlans, string resourceGroupName)
        {        
            foreach (var appServicePlan in appServicePlans)
            {
                var plan = new AppServicePlan(appServicePlan.Name, new AppServicePlanArgs
                {
                    Name              = appServicePlan.Name,
                    ResourceGroupName = resourceGroupName,
                    Location          = _specification.RegionName,
                    Kind              = appServicePlan.Kind,
                    Reserved          = true,
                    Sku               = new SkuDescriptionArgs {  Name = appServicePlan.Sku, 
                                                                  Tier = appServicePlan.Sku switch
                                                                  {
                                                                     "P1v2" => "PremiumV2",
                                                                     _ => throw new NotSupportedException()
                                                                  },
                                                                  Family = appServicePlan.Sku switch
                                                                  {
                                                                     "P1v2" => "Pv2",
                                                                     _ => throw new NotSupportedException()
                                                                  }, 
                                                                  Size = appServicePlan.Sku
                                                               },
                                      
                    Tags              = this.Tags.Items
                });

                var options = new CustomResourceOptions { DependsOn = plan };

                if(appServicePlan.FunctionApps != null) 
                {
                    DeployFunctionApps(appServicePlan.FunctionApps, plan, resourceGroupName, options);
                }
            }
        }

        private void DeployFunctionApps(IEnumerable<FunctionAppSpecification> functionApps, AppServicePlan plan, string resourceGroupName, CustomResourceOptions options)
        {        
            var appInsights = GetComponent.Invoke( new GetComponentInvokeArgs 
            {
                ResourceGroupName = resourceGroupName,
                ResourceName = $"{_prefix}-ain"
            });

            var environmentCode = _specification.EnvironmentCode;
            var regionCode = _specification.RegionCode;

            foreach(var functionApp in functionApps)
            {
                var appSettings = new AppSettings
                {
                    Items = 
                    {
                        // These are base config entries needed for an Azure Function
                        new NameValuePairArgs { Name = "FUNCTIONS_EXTENSION_VERSION",           Value = "~4" },
                        new NameValuePairArgs { Name = "FUNCTIONS_WORKER_RUNTIME",              Value = "dotnet-isolated" },
                        new NameValuePairArgs { Name = "APPINSIGHTS_INSTRUMENTATIONKEY",        Value = appInsights.Apply( ai=> ai.InstrumentationKey) },
                        new NameValuePairArgs { Name = "APPLICATIONINSIGHTS_CONNECTION_STRING", Value = appInsights.Apply( ai=> ai.ConnectionString) },
                        new NameValuePairArgs { Name = "SCM_DO_BUILD_DURING_DEPLOYMENT",        Value = "0" },
                        new NameValuePairArgs { Name = "AzureWebJobsStorage",                   Value = KeyVaultReference("AzureWebJobsStorage") }
                    }
                };

                // If we call internal APIs then add Auth0 attributes
                if(functionApp.CallsCoreServices)
                {
                    var domain = regionCode.StartsWith("au") ? "au.auth0" : "auth0";

                    appSettings.Items.Add(new NameValuePairArgs { Name = "M2MAuthAudience",     Value = environmentCode == "prd" ? "https://willowtwin-web" : $"https://willowtwin-web-{environmentCode}" });
                    appSettings.Items.Add(new NameValuePairArgs { Name = "M2MAuthClientId",     Value = KeyVaultReference("Auth0-ClientId") });
                    appSettings.Items.Add(new NameValuePairArgs { Name = "M2MAuthDomain",       Value = $"willowtwin-{environmentCode}.{domain}.com" });
                    appSettings.Items.Add(new NameValuePairArgs { Name = "M2MAuthClientSecret", Value = KeyVaultReference("Auth0-ClientSecret") });
                }

                // Add settings from Specification
                if(functionApp.AppSettings != null)
                    foreach(var setting in functionApp.AppSettings)
                        appSettings.Items.Add(new NameValuePairArgs { Name = setting.Key, Value = setting.Value });

                var app = new WebApp(functionApp.Name, new WebAppArgs
                {
                    Name                = functionApp.Name,
                    ResourceGroupName   = resourceGroupName,
                    Location            = plan.Location,
                    Kind                = "functionapp,linux",
                    ServerFarmId        = plan.Id,
                    Identity            = new ManagedServiceIdentityArgs { Type = ManagedServiceIdentityType.SystemAssigned },
                    Reserved            = true,
                    SiteConfig          = new SiteConfigArgs
                    {
                        LinuxFxVersion              = "DOTNET-ISOLATED|6.0",
                        AlwaysOn                    = true,
                        FunctionAppScaleLimit       = 200,
                        MinimumElasticInstanceCount = 1,
                        AppSettings                 = appSettings.Items
                    }
                },
                options);

                var principalId = app.Identity.Apply( i=> i.PrincipalId);
                _newResourcePrincipalIds.Add(functionApp.Name, principalId);
                _newResources.Add(functionApp.Name, app);

                // Set up access to KeyVault
                DeployRoleAssignment(principalId, functionApp.Name, _newKeyVaultId, _newKeyVaultName, Roles.KeyVault.SecretReader, options: new CustomResourceOptions { DependsOn = app });
            }
        }

        private void UpdateExistingStorageAccounts(IEnumerable<StorageAccountSpecification> storageAccounts, string resourceGroupName)
        {
            foreach (var storageAccount in storageAccounts)
            {
                var acct = GetStorageAccount.Invoke(new GetStorageAccountInvokeArgs
                {
                    AccountName = storageAccount.Name,
                    ResourceGroupName = resourceGroupName
                });

                // Deploy role assignments for this storage accounts
                DeployRoleAssignments(storageAccount.RoleAssignments, acct.Apply( a=> a.Id ), storageAccount.Name, Roles.Storage.BlobDataContributor, resourceGroupName, null);
            }            
        }

        private void DeployStorageAccounts(IEnumerable<StorageAccountSpecification> storageAccounts, string resourceGroupName)
        {
            foreach (var storageAccount in storageAccounts)
            {
                DeployStorageAccount(storageAccount, resourceGroupName);
            }        
        }

        private StorageAccount DeployStorageAccount(StorageAccountSpecification storageAccount, string resourceGroupName)
        {
            var acct = new StorageAccount(storageAccount.Name, new StorageAccountArgs
            {
                AccountName            = storageAccount.Name,
                ResourceGroupName      = resourceGroupName,
                Location               = _specification.RegionName,
                Kind                   = storageAccount.Kind,
                MinimumTlsVersion      = "TLS1_2",
                Sku                    = new Pulumi.AzureNative.Storage.Inputs.SkuArgs { Name = storageAccount.Sku },
                AllowBlobPublicAccess  = true,
                AllowSharedKeyAccess   = true,
                EnableHttpsTrafficOnly = true,
                Tags                   = this.Tags.Items
            });

            var options = new CustomResourceOptions { DependsOn = acct };

            // If any of the containers require public access then we need to set up CORS
            if (storageAccount.Containers?.Where(c => c.AllowPublicAccess)?.Any() ?? false)
            {
                SetupCORS(acct.Name, resourceGroupName, options);
            }

            // Create storage containers
            if (storageAccount.Containers != null)
            {
                foreach (var container in storageAccount.Containers)
                {
                    _ = new BlobContainer(container.Name, new BlobContainerArgs
                    {
                        AccountName       = acct.Name,
                        ContainerName     = container.Name,
                        ResourceGroupName = resourceGroupName,
                        PublicAccess      = container.AllowPublicAccess ? PublicAccess.Blob : PublicAccess.None,
                    },
                    options);
                }
            }

            // Add all previous apps created just in case
            var dependsOn = _newResources.Values.ToList();

            dependsOn.Add(acct);

            var newOptions = new CustomResourceOptions { DependsOn = dependsOn };

            // Deploy role assignments for this storage accounts
            DeployRoleAssignments(storageAccount.RoleAssignments, acct.Id, storageAccount.Name, Roles.Storage.BlobDataContributor, resourceGroupName, newOptions);

            // Give the current service connection access to this storage account
            if(storageAccount.GiveServiceConnectionAccess)
            {
               DeployRoleAssignment("f9624c1d-234a-4527-8fd6-6ca04a8dfa6e", // This is the object id of the service principal "azdo-global-spn"
                                    "azdo-global-spn", 
                                    acct.Id, 
                                    storageAccount.Name,
                                    Roles.Storage.BlobDataContributor, 
                                    options: newOptions);
            }

            return acct;
        }

        private void DeployServiceBusNamespaces(IEnumerable<ServiceBusSpecification> serviceBusNamespaces, string resourceGroupName)
        {
            foreach (var serviceBus in serviceBusNamespaces)
            {
                var sbNamespace = new ServiceBus.Namespace(serviceBus.Name, new ServiceBus.NamespaceArgs
                {
                    NamespaceName     = serviceBus.Name,
                    ResourceGroupName = resourceGroupName,
                    Location          = _specification.RegionName,
                    Sku               = new SBSkuArgs { Name = serviceBus.Sku switch
                                                        {
                                                            "Basic"    => ServiceBus.SkuName.Basic,
                                                            "Standard" => ServiceBus.SkuName.Standard,
                                                            "Premium"  => ServiceBus.SkuName.Premium,
                                                            _ => throw new ArgumentException("Unknown ServiceBus SKU")
                                                        }, 
                                                        Tier = serviceBus.Sku switch
                                                        {
                                                            "Basic"    => ServiceBus.SkuTier.Basic,
                                                            "Standard" => ServiceBus.SkuTier.Standard,
                                                            "Premium"  => ServiceBus.SkuTier.Premium,
                                                            _ => throw new ArgumentException("Unknown ServiceBus Tier")
                                                        }
                                                      },
                    Tags              = this.Tags.Items
                });

                var options = new CustomResourceOptions { DependsOn = sbNamespace };

                // Create queues
                if (serviceBus.Queues != null)
                {
                    foreach (var queue in serviceBus.Queues)
                    {
                        _ = new ServiceBus.Queue(queue, new ServiceBus.QueueArgs
                        {
                            QueueName                        = queue,
                            NamespaceName                    = sbNamespace.Name,
                            ResourceGroupName                = resourceGroupName,
                            EnablePartitioning               = false,
                            DeadLetteringOnMessageExpiration = true,
                            MaxDeliveryCount                 = 20

                        },
                        options);
                    }
                }

                // Add all previous apps created just in case
                var dependsOn = _newResources.Values.ToList();

                dependsOn.Add(sbNamespace);

                var newOptions = new CustomResourceOptions { DependsOn = dependsOn };

                // Deploy role assignments for this Service Bus namespace
                DeployRoleAssignments(serviceBus.RoleAssignments.Where( r=> r.Listener), sbNamespace.Id, serviceBus.Name, Roles.ServiceBus.DataListener, resourceGroupName, newOptions);
                DeployRoleAssignments(serviceBus.RoleAssignments.Where( r=> r.Sender), sbNamespace.Id, serviceBus.Name, Roles.ServiceBus.DataSender, resourceGroupName, newOptions);
            }
        }

        private void DeployRoleAssignments(IEnumerable<RoleAssignmentSpecification> assignments, Output<string> scope, string scopeName, string roleId, string resourceGroupName, CustomResourceOptions options, string nameSuffix = "")
        {
            if (assignments != null)
            {
                foreach (var assignment in assignments)
                {
                    var targetResource = assignment.ExistingResource?.ResourceName ?? assignment.NewResource;
                    var envRegionCode  = $"{_specification.EnvironmentCode}.{_specification.RegionCode}";
                    Output<string> principalId;
                    
                    if(assignment.ExistingResource != null)
                    { 
                        resourceGroupName = ResolveResourceGroup(assignment.ExistingResource, resourceGroupName, envRegionCode);

                        // This will work for both app services and function apps
                        principalId = GetWebApp.Invoke(new GetWebAppInvokeArgs 
                                                           { 
                                                               Name = targetResource, 
                                                               ResourceGroupName = resourceGroupName
                                                           }
                                                          ).Apply( app=> app.Identity?.PrincipalId );
                    }
                    else
                        principalId = _newResourcePrincipalIds[assignment.NewResource];

                    DeployRoleAssignment(principalId, targetResource, scope, scopeName, roleId, options: options, nameSuffix: nameSuffix);
                }
            }
        }

        /// <summary>
        /// Add a role assignment
        /// </summary>
        /// <param name="targetPrincipalId">Resource that needs access</param>
        /// <param name="scope">Resource that you're accessing</param>
        private void DeployRoleAssignment(Input<string> targetPrincipalId, string targetResourceName, Output<string> scope, string scopeName, string roleId, PrincipalType? principalType = null, CustomResourceOptions options = null, string nameSuffix = "")
        {
            var name = $"t3-{_prefix}-{targetResourceName + nameSuffix}-{scopeName}-{roleId}-roleassign-id";

            if(options != null)
                options.DeleteBeforeReplace = true;
            else 
                options = new CustomResourceOptions { DeleteBeforeReplace = true };    

            _ = new RoleAssignment(name, new RoleAssignmentArgs
            {
                RoleAssignmentName = new RandomUuid(name).Id,
                RoleDefinitionId = $"/subscriptions/{Environment.GetEnvironmentVariable("ARM_SUBSCRIPTION_ID")}/providers/Microsoft.Authorization/roleDefinitions/{roleId}",
                PrincipalId = targetPrincipalId,
                PrincipalType = principalType ?? PrincipalType.ServicePrincipal,
                Scope = scope
            },
            options);
        }

        private void SetupCORS(Output<string> accountName, string resourceGroupName, CustomResourceOptions options)
        {        
            _ = new BlobServiceProperties
            (
                "allowpublicaccess",
                new BlobServicePropertiesArgs
                {
                    AccountName      = accountName,
                    BlobServicesName = "default",
                    Cors             = new CorsRulesArgs
                    {
                        CorsRules = new List<CorsRuleArgs>
                        {
                            new CorsRuleArgs
                            {
                                AllowedOrigins  = new List<string> { "*" },
                                AllowedHeaders  = new List<string> { "x-ms-meta-abc", "x-ms-meta-data*", "x-ms-meta-target*" },
                                AllowedMethods  = "GET",
                                ExposedHeaders  = new List<string> { "x-ms-meta-*" },
                                MaxAgeInSeconds = 200
                            }
                        }
                    },
                    ResourceGroupName = resourceGroupName
                },
                options
            );
        }

        private static class Roles
        { 
            internal static class Storage
            { 
                internal static readonly string BlobDataContributor = "ba92f5b4-2d11-453d-a403-e96b0029c9fe";
            }

            internal static class ServiceBus
            { 
                internal static readonly string DataListener = "4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0";
                internal static readonly string DataSender   = "69a216fc-b8fb-44d8-bc22-1f3c2cd27a39";
            }

            internal static class KeyVault
            { 
                internal static readonly string SecretOfficer = "b86a8fe4-44ce-4948-aee5-eccb2c155cd7";
                internal static readonly string SecretReader  = "4633458b-17de-408a-b874-0445c86b69e6";
            }
        }

        private class AppSettings
        {
            internal InputList<NameValuePairArgs> Items { get; init; } = new InputList<NameValuePairArgs>();
        }

        private Tags Tags => new Tags
        {
            Items = {
                        { "company",     "willow" },
                        { "environment", _specification.EnvironmentCode },
                        { "location",    _specification.RegionCode },
                        { "project",     "platform" },
                        { "team",        "realestate" },
                        { "customer",    "shared" },
                        { "managedby",   "pulumi" }
                    }
        };
       
        private string ResolveResourceGroup(ExistingResourceSpecification resource, string resourceGroupName, string envRegionCode)
        {
            if(resource.RegionalResourceGroups != null && resource.RegionalResourceGroups.ContainsKey(envRegionCode))
                resourceGroupName = resource.RegionalResourceGroups[envRegionCode];
            else if(!string.IsNullOrWhiteSpace(resource.ResourceGroupName))
                resourceGroupName = resource.ResourceGroupName;

            return resourceGroupName;
        }

        private string KeyVaultReference(string secretName)
        {
            return $"@Microsoft.KeyVault(VaultName={_newKeyVaultName};SecretName={secretName})";
        }

        #endregion
    }

    internal class Tags
    {
        internal InputMap<string> Items { get; init; } = new InputMap<string>();
    }
}
