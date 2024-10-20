### Run Locally

##### Install Pulumi
https://www.pulumi.com/docs/get-started/azure/begin/

##### Install .Net SDK
https://dotnet.microsoft.com/en-us/download

##### Login to Azure
az login

##### Select Azure subscription
az account set --subscription [subscription name or id]

##### Change Directory
cd [this directory]

##### Preview
pulumi preview

If you have not done peviously this will ask to create a stack. Type in the name of the environment you want to deploy to: [dev, uat or prd]

##### Deploy
pulumi up

Select stack (environment) you created in previous step


