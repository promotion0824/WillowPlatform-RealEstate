using Pulumi;
using System;
using System.Collections.Generic;

namespace Willow.RealEstate.Deployment
{
    internal class ExistingResourceSpecification 
    {
        internal string       ResourceName      { get; init; }
        internal ResourceType ResourceType      { get; init; } = ResourceType.AppService;
        internal string       ResourceGroupName { get; init; }
        internal Dictionary<string, string> RegionalResourceGroups { get; init; }
    }

    internal enum ResourceType
    {
        AppService,
        FunctionApp
    }
 }
