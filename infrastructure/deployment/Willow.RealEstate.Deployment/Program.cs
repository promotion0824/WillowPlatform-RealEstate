using System.Threading.Tasks;

namespace Willow.RealEstate.Deployment
{
	class Program
	{
		static Task<int> Main() => Pulumi.Deployment.RunAsync<DeploymentStack>();
	}
}