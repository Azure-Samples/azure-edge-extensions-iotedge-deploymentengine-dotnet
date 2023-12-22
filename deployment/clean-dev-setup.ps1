Param (

    [Parameter(Mandatory=$true)]
    [string]$resourcesPrefix
)

Set-StrictMode -Version 2.0

# Delete Resource group
$resourceGroupName = "rg-$resourcesPrefix"
Write-Host "About to delete resource group '$resourceGroupName'"
az group delete --name $resourceGroupName

# Delete App registrations
$firstAppName = $resourcesPrefix + "App"
$appNamePostman= $resourcesPrefix + "Postman"

$appReg = (az ad app list --display-name $firstAppName) | ConvertFrom-Json
Write-Host "About to delete AD app & SP '$firstAppName'"
az ad app delete --id $appReg.id
$appRegPostman = (az ad app list --display-name $appNamePostman) | ConvertFrom-Json
Write-Host "About to delete AD app & SP '$appNamePostman'"
az ad app delete --id $appRegPostman.id

Write-Host "Deleted resource group '$resourceGroupName' and app registrations '$firstAppName' and '$appNamePostman'"
