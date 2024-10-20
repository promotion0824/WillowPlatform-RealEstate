function import {
    param(
        $StorageAccount,
        $StackName,
        $Customer,
        $CustomerResourceGroup,
        $CustomerSubscription,
        $AdxDatabase = $null,
        $AdxCluster = $null,
        $AdxResourceGroup = $null,
        $AdxSubscription = $null,
        $adtName = $null
    )

    Push-Location RealEstate.Customers

    $split = $StackName.Split(".")

    $environment = $split[0]
    $region = $split[1]

    if($environment -eq 'dev') {
        $subscription = 'Platform-DEV' 
    } elseif($environment -eq 'uat') {
        $subscription = 'Platform-UAT' 
    } else {
        $subscription = 'Platform-PRD'
    }

    Write-Output "Importing $environment $Customer"

    $adtName = if ($null -eq $adtName) { "wil-$environment-lda-$Customer-$region-adt" } else { $adtName }
    $adtId = az dt show -n $adtName -g $CustomerResourceGroup --subscription $CustomerSubscription --query "id"
    Write-Output $adtId
    $keyVaultName = "Platform$($environment)DeployKeys"
    $keyName = "pulumi"

    $env:AZURE_KEYVAULT_AUTH_VIA_CLI=$true

    $accountKey = az storage account keys list --account-name $StorageAccount --subscription $subscription --resource-group deployment-data --query [0].value -o tsv

    $env:AZURE_STORAGE_ACCOUNT=$StorageAccount
    $env:AZURE_STORAGE_SAS_TOKEN=$(az storage container generate-sas --account-key $accountKey --account-name $env:AZURE_STORAGE_ACCOUNT --name "pulumi" --permissions acdlmrtw --expiry $((Get-date).AddDays(1) | ForEach-Object { "$($_.Year)-$($_.Month)-$($_.Day)" }) --auth-mode key).Trim('"')
    pulumi login azblob://pulumi
    pulumi stack init $StackName --secrets-provider="azurekeyvault://$keyVaultName.vault.azure.net/keys/$keyName"
    pulumi stack select $StackName
    pulumi stack change-secrets-provider "azurekeyvault://$keyVaultName.vault.azure.net/keys/$keyName"
    pulumi import --stack $StackName azure-native:digitaltwins:DigitalTwin $adtName $adtId -y

    if ($AdxDatabase -and $AdxCluster -and $AdxResourceGroup) {
        $adxId = az kusto database show --database-name $AdxDatabase --resource-group $AdxResourceGroup --cluster-name $AdxCluster --subscription $AdxSubscription --query "id"
        Write-Output $adxId
        pulumi import --stack $StackName azure-native:kusto:ReadWriteDatabase $AdxCluster/$AdxDatabase $adxId -y
    }

    Pop-Location
}

clear

# import platformdevdeploydata sbx.aue1 sbx t3-wil-sbx-lda-sbx-aue1-app-rsg SandboxShared

# import platformdevdeploydata dev.eu21 brf t3-wil-dev-lda-brf-eu21-app-rsg Platform-DEV
# import platformdevdeploydata dev.eu21 msft t3-wil-dev-lda-msft-eu21-app-rsg Platform-DEV
# import platformdevdeploydata dev.aue1 inv t3-wil-dev-lda-inv-aue1-app-rgs Platform-DEV

# import platformuatdeploydata uat.eu21 brf t3-wil-uat-lda-brf-eu21-app-rsg Platform-UAT Brookfield-UAT wilnonprodaueadx nonprod-platformdata K8S-INTERNAL
# import platformuatdeploydata uat.eu21 dfw t3-wil-uat-lda-dfw-eu21-app-rsg Platform-UAT DallasFortWorthAirport-UAT wilnonprodaueadx nonprod-platformdata K8S-INTERNAL
# import platformuatdeploydata uat.aue1 inv t3-wil-uat-lda-inv-aue1-app-rgs Platform-UAT Investa-UAT wilnonprodaueadx nonprod-platformdata K8S-INTERNAL

# import platformprddeploydata prd.eu22 wmc t3-wil-prd-lda-wmc-eu22-app-rsg Platform-PRD WatermanClark-PRD wilprodeu2adx prod-platformdata-eu2 Products-Shared
# import platformprddeploydata prd.eu22 brf t3-wil-prd-lda-brf-eu22-app-rsg Platform-PRD Brookfield-PRD wilprodeu2adx prod-platformdata-eu2 Products-Shared wil-prd-lda-brf-eu22-adt2
# import platformprddeploydata prd.eu22 dfw t3-wil-prd-lda-dfw-eu22-app-rsg Platform-PRD DallasFortWorthAirport-PRD wilprodeu2adx prod-platformdata-eu2 Products-Shared
# import platformprddeploydata prd.eu22 durst t3-wil-prd-lda-durst-eu22-app-rsg Platform-PRD Durst-PRD wilprodeu2adx prod-platformdata-eu2 Products-Shared
# import platformprddeploydata prd.eu22 jpmc t3-wil-prd-lda-jpmc-eu22-app-rsg Platform-PRD JPMorganChase-PRD wilprodeu2adx prod-platformdata-eu2 Products-Shared
# import platformprddeploydata prd.eu22 mcm t3-wil-prd-lda-mcm-eu22-app-rsg Platform-PRD MicrosoftEastCampus-PRD wilprodeu2adx prod-platformdata-eu2 Products-Shared
# import platformprddeploydata prd.eu22 msft t3-wil-prd-lda-msft-eu22-app-rsg Platform-PRD Microsoft-PRD wilprodeu2adx prod-platformdata-eu2 Products-Shared
# import platformprddeploydata prd.eu22 oxf t3-wil-prd-lda-oxf-eu22-app-rsg Platform-PRD Oxford-PRD wilprodeu2adx prod-platformdata-eu2 Products-Shared
# import platformprddeploydata prd.aue2 wlo t3-wil-prd-lda-wlo-aue2-app-rsg Platform-PRD Willow-PRD wilprodaueadx prod-platformdata-aue Products-Shared
# import platformprddeploydata prd.aue2 mmp t3-wil-prd-lda-mmp-aue2-app-rsg Platform-PRD Macquarie-PRD wilprodaueadx prod-platformdata-aue Products-Shared
# import platformprddeploydata prd.aue2 inv t3-wil-prd-lda-inv-aue2-app-rsg Platform-PRD Investa-PRD wilprodaueadx prod-platformdata-aue Products-Shared
# import platformprddeploydata prd.aue2 wsa t3-wil-prd-lda-wsa-aue2-app-rsg Platform-PRD 
# import platformprddeploydata prd.aue2 met t3-wil-prd-lda-met-aue2-app-rsg Platform-PRD 
# import platformprddeploydata prd.weu2 axa t3-wil-prd-lda-axa-weu2-app-rsg Platform-PRD AXA-PRD wilprodweuadx prod-platformdata-weu Products-Shared