using RealEstate.Customers.Stacks;
using Deployment = Pulumi.Deployment;

return await Deployment.RunAsync<CustomerStack>();