using System;
using Pulumi;

namespace RealEstate.Customers.Components.Tags
{
    public class TagBuilder
    {
        private readonly InputMap<string> _defaultTags;

        private string Environment { get; }

        public TagBuilder(string app, string customer = "none", string environment = null)
        {
            Environment = environment;
            _defaultTags = new InputMap<string>
            {
                {"stack", System.Environment.GetEnvironmentVariable("STACK_NAME") ?? Deployment.Instance.StackName},
                {"company", "willow"},
                {"customer", customer},
                {"app", app},
                {"managedby", "pulumi"},
                {"team", "real estate"},
                {"environment", Environment ?? System.Environment.GetEnvironmentVariable("ENVIRONMENT_NAME") ?? "UNKOWN"},
                {"project", System.Environment.GetEnvironmentVariable("DEFINITION_LOCATION") ?? System.Environment.GetEnvironmentVariable("BUILD_REPOSITORY_URI") ?? "willowdev/WillowPlatform"},
                {"created", $"{DateTimeOffset.UtcNow}"},
                {"PipelineName", System.Environment.GetEnvironmentVariable("DEFINITION_NAME") ??System.Environment.GetEnvironmentVariable("BUILD_DEFINITIONNAME") ?? System.Environment.GetEnvironmentVariable("RELEASE_DEFINITIONNAME") ?? "UNKOWN"},
                {"PipelineId", System.Environment.GetEnvironmentVariable("DEFINITION_ID") ??System.Environment.GetEnvironmentVariable("BUILD_BUILDNUMBER") ?? System.Environment.GetEnvironmentVariable("RELEASE_RELEASENAME") ?? "UNKOWN"}
            };
        }

        TagBuilder WithExtraTag(string tag, string value)
        {
            _defaultTags.Add(tag, value);
            return this;
        }
        
        public InputMap<string> Build()
        {
            return _defaultTags;
        }
    }
}
