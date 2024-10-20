Param(
    [Parameter(Mandatory=$true)]
    [string]
    $StorageAccount,
    [Parameter(Mandatory=$true)]
    [string]
    $StackName,
    [switch]
    $local
)

Push-Location RealEstate.Customers

$split = $StackName.Split(".")

$environment = $split[0]

if($environment -eq 'dev') {
    $subscription = 'Platform-DEV' 
} elseif($environment -eq 'uat') {
    $subscription = 'Platform-UAT' 
} else {
    $subscription = 'Platform-PRD'
}

if($local) {
    pulumi login --local
    pulumi preview --stack $StackName --json
} else {
    $env:AZURE_KEYVAULT_AUTH_VIA_CLI=$true

    $accountKey = az storage account keys list --account-name $StorageAccount --subscription $subscription --resource-group deployment-data --query [0].value -o tsv

    $env:AZURE_STORAGE_ACCOUNT=$StorageAccount
    $env:AZURE_STORAGE_SAS_TOKEN=$(az storage container generate-sas --account-key $accountKey --account-name $env:AZURE_STORAGE_ACCOUNT --name "pulumi" --permissions acdlmrtw --expiry $((Get-date).AddDays(1) | ForEach-Object { "$($_.Year)-$($_.Month)-$($_.Day)" }) --auth-mode key).Trim('"')
    pulumi login azblob://pulumi
    pulumi preview --stack $StackName --show-replacement-steps
}

Pop-Location