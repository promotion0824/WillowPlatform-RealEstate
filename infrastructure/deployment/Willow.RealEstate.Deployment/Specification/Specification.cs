using Pulumi;
using System;
using System.Collections.Generic;

namespace Willow.RealEstate.Deployment
{
    internal static partial class RealEstate 
    {
        internal static StackSpecification BuildStackSpecification(string environmentCode, string regionCode)
        {
            var prefix              = $"wil-{environmentCode}-plt-{regionCode}";
            var KeyVaultReference   = $"@Microsoft.KeyVault(VaultName=wil-{environmentCode}-plt-{regionCode}-kvl2;SecretName=" + "{0})";
            var SiteBaseAddress     = $"https://{prefix}-" + "{0}.azurewebsites.net";
            var existingResources   = new Dictionary<string,ExistingResourceSpecification>
            {
                { "portalxl",        new ExistingResourceSpecification() { ResourceName = $"{prefix}-portalxl" } },
                { "mobilexl",        new ExistingResourceSpecification() { ResourceName = $"{prefix}-mobilexl" } },
                { "imagehub",        new ExistingResourceSpecification() { ResourceName = $"{prefix}-imagehub" } },
                { "assetcore",       new ExistingResourceSpecification() { ResourceName = $"{prefix}-assetcore" } },
                { "directorycore",   new ExistingResourceSpecification() { ResourceName = $"{prefix}-directorycore" } },
                { "workflowcore",    new ExistingResourceSpecification() { ResourceName = $"{prefix}-workflowcore" } },
                { "sitecore",        new ExistingResourceSpecification() { ResourceName = $"{prefix}-sitecore" } },
                { "insightscore",    new ExistingResourceSpecification() { ResourceName = $"{prefix}-insightcore" } },
                { "publicapi",       new ExistingResourceSpecification() { ResourceName = $"{prefix}-publicapi" } },
                { "digitaltwincore", new ExistingResourceSpecification() { 
                                                                           ResourceName           = $"{prefix}-digitaltwincore", 
                                                                           RegionalResourceGroups = new Dictionary<string, string>
                                                                           {
                                                                               { "uat.aue1", $"t3-wil-uat-plt-aue1-app2-rsg" },
                                                                               { "prd.aue2", $"t3-wil-prd-plt-aue2-app2-rsg" }
                                                                           }
                                                                         }}
            };
        
            var newResources = new Dictionary<string, string>
            {
                { "commsvc", $"{prefix}-commsvc-func" }
            };
        
            return new StackSpecification
            {
                EnvironmentCode = environmentCode, 
                RegionCode = regionCode, 
                RegionName = regionCode switch 
                {
                    "aue1" => "Australia East",
                    "eu21" => "East US 2",
                    "aue2" => "Australia East",
                    "eu22" => "East US 2",
                    "weu2" => "West Europe", 
                    _ => throw new ArgumentException("Invalid region")                
                }, 

                ExistingApps = existingResources,
                ResourceGroups = new List<ResourceGroupSpecification>
                {
                    new ResourceGroupSpecification
                    {
                        Name = $"t3-{prefix}-app-rsg",

                        StorageAccounts = new List<StorageAccountSpecification>
                        {
                            // Content storage
                            new StorageAccountSpecification
                            {
                                Name = $"wil{environmentCode}plt{regionCode}contentsto",
                                GiveServiceConnectionAccess = true,
                                RoleAssignments = new List<RoleAssignmentSpecification>
                                {
                                    new RoleAssignmentSpecification() { ExistingResource = existingResources["portalxl"] },
                                    new RoleAssignmentSpecification() { ExistingResource = existingResources["imagehub"] },
                                    new RoleAssignmentSpecification() { ExistingResource = existingResources["digitaltwincore"] },
                                    new RoleAssignmentSpecification() { NewResource      = newResources["commsvc"] }
                                },
                                Containers = new List<StorageContainerSpecification>
                                {
                                    new StorageContainerSpecification
                                    {
                                        Name = "realestate",
                                        AllowPublicAccess = false
                                    },
                                    new StorageContainerSpecification
                                    {
                                        Name = "realestateui",
                                        AllowPublicAccess = true
                                    }
                                }
                            }
                        },

                        ExistingStorageAccounts = new List<StorageAccountSpecification>
                        {
                            // Content storage
                            new StorageAccountSpecification
                            {
                                Name = $"wil{environmentCode}plt{regionCode}imagehub",
                                RoleAssignments = new List<RoleAssignmentSpecification>
                                {
                                    new RoleAssignmentSpecification() { ExistingResource = existingResources["imagehub"] },
                                }
                            }
                        },

                        AppServicePlans = new List<AppServicePlanSpecification> 
                        {
                            new AppServicePlanSpecification
                            {
                                Name  = $"{prefix}-functions-asp",

                                FunctionApps = new List<FunctionAppSpecification>
                                {
                                    new FunctionAppSpecification 
                                    {  
                                        Name = newResources["commsvc"],
                                        CallsCoreServices = true,
                                        AppSettings = new Dictionary<string, string>    
                                        {
                                            { "EmailFromAddress",                  "no-reply@willowinc.com" },
                                            { "EmailFromName",                     "Willow" },
                                            { "SendGridApiKey",                    string.Format(KeyVaultReference, "SendGridApiKey") },
                                                                                   
                                            { "DirectoryCoreBaseAddress",          string.Format(SiteBaseAddress, "directorycore") },
                                                                                   
                                            { "Azure.BlobStorage.AccountName",     $"wil{environmentCode}plt{regionCode}sto" },
                                            { "Azure.BlobStorage.ContainerName",   "realestate" },
                                                                                   
                                            { "ServiceBusNamespace",               $"{prefix}-sbns" },
                                            { "ServiceBusConnectionString",        $"Endpoint=sb://{prefix}-sbns.servicebus.windows.net/;Authentication=ManagedIdentity" },
                                                                                   
                                            { "PushNotification.ConnectionString", string.Format(KeyVaultReference, "NotificationHubConnectionString") },
                                            { "PushNotification.HubPath",          $"{prefix}-nhb" },
                                        }
                                    }
                                }
                            }
                        },

                        ServiceBusNamespaces = new List<ServiceBusSpecification>
                        {
                            new ServiceBusSpecification
                            {
                                Name = $"wil-{environmentCode}-plt-{regionCode}-sbns",

                                Queues = new List<string> { "commsvc", "pushinstallation" },

                                RoleAssignments = new List<ServiceBusRoleAssignment> 
                                {
                                    new ServiceBusRoleAssignment { ExistingResource = existingResources["portalxl"],      Sender = true },
                                    new ServiceBusRoleAssignment { ExistingResource = existingResources["mobilexl"],      Sender = true },
                                    new ServiceBusRoleAssignment { ExistingResource = existingResources["workflowcore"],  Sender = true },
                                    new ServiceBusRoleAssignment { ExistingResource = existingResources["directorycore"], Sender = true },
                                    new ServiceBusRoleAssignment { NewResource      = newResources["commsvc"],            Listener = true }
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
