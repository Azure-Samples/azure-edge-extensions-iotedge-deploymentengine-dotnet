Param (

    [Parameter(Mandatory=$false)]
    [string]$location = "West Europe",

    [Parameter(Mandatory=$true)]
    [string]$resourcesPrefix,

    [Parameter(Mandatory=$true)]
    [string]$tenantName
)

Set-StrictMode -Version 2.0

Import-Module -Name ./modules/appRegistration.psm1 -Force

$ErrorActionPreference = "Stop"

# Get info ===========================

$account = $(az account show -o json | ConvertFrom-Json)
$tenantId = $account.tenantId
$subscriptionId = $account.id
$currentUserId = $(az ad signed-in-user show --query id | ConvertFrom-Json)
$functionSiteName = "func-$resourcesPrefix"

# AD App registrations ===========================

$firstAppName = $resourcesPrefix + "App"
$appNamePostman= $resourcesPrefix + "Postman"
Write-Host "Creating AD app registrations $firstAppName and $appNamePostman"

$firstApp = RegisterApp -TenantName "$tenantName"  -AppName "$firstAppName"  -FunctionName "$functionSiteName"
$postmanApp = RegisterApp -TenantName "$tenantName"  -AppName "$appNamePostman"  -FunctionName "$functionSiteName"

# ARM ===========================

Write-Host "Deploying ARM template"
$resourceGroupName = "rg-$resourcesPrefix"
az group create -n $resourceGroupName --location $location -o none

$deployment = (az deployment group create --name azuredeploy --resource-group $resourceGroupName `
    --template-file azuredeploy.json `
    --parameters resourceName=$resourcesPrefix tenantId=$tenantId appId=$firstApp `
    ) | ConvertFrom-Json

$iotHubHostname = $deployment.properties.outputs.iotHubHostName.value
$iotHubName = $deployment.properties.outputs.iotHubName.value
$keyVaultName = $deployment.properties.outputs.keyVaultName.value
$keyVaultUri = "https://$keyVaultName.vault.azure.net/"
$defaultManifests = Join-Path -Path (Split-Path (Get-Location) -Parent) -ChildPath "manifests"

# Assigning roles and keyvault ===========================

Write-Host "Adding current signed-in user RBAC roles to IoT Hub and Key Vault resources"

az role assignment create --assignee $currentUserId `
    --role "Key Vault Secrets Officer" `
    --scope "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.KeyVault/vaults/$keyVaultName" -o none

az role assignment create --assignee $currentUserId `
    --role "IoT Hub Data Contributor" `
    --scope "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.Devices/IotHubs/$iotHubName" -o none

az role assignment create --assignee $currentUserId `
    --role "IoT Hub Registry Contributor" `
    --scope "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.Devices/IotHubs/$iotHubName" -o none

az role assignment create --assignee $currentUserId `
    --role "IoT Hub Twin Contributor" `
    --scope "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.Devices/IotHubs/$iotHubName" -o none

Write-Host "Creating two IoT Edge devices with symmetric auth and two tags"
az iot hub device-identity create -n $iotHubName -d device100 --ee  -o none
az iot hub device-twin update -n $iotHubName -d device100 --tags '{\"env\": \"device100\"}'  -o none
az iot hub device-identity create -n $iotHubName -d device101 --ee  -o none
az iot hub device-twin update -n $iotHubName -d device101 --tags '{\"iiot\": true}' -o none

Write-Host "Creating Key Vault sample secrets 'address, password, username'"
az keyvault secret set --name address --vault-name $keyVaultName --value "registry:5000" -o none
az keyvault secret set --name password --vault-name $keyVaultName --value "password" -o none
az keyvault secret set --name username --vault-name $keyVaultName --value "username" -o none

$authUri = "$firstApp.$tenantName.onmicrosoft.com"

Write-Host "Values you can use to create .env and local.settings.json files for your dev environment:"
Write-Host "============="
Write-Host "IOTHUB_HOSTNAME=$iotHubHostName"
Write-Host "KEYVAULT_URI=$keyVaultUri"
Write-Host "ROOT_MANIFESTS_FOLDER=$defaultManifests"
Write-Host "Below only required for local.settings.json"
Write-Host "OpenApi__Auth__TenantId = $tenantId"
Write-Host "OpenApi__Auth__Scope = https://$authUri/user_impersonation"
Write-Host "OpenApi__Auth__Audience = $firstApp"
Write-Host "============="

Write-Host "Azure Resources group with all resources: '$resourceGroupName'"

# End ===========================