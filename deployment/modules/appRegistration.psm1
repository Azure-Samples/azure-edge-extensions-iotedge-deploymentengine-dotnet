# Function to register AD app and service principal

Function RegisterApp #([string]$tenantName, [string]$appName,[string]$functionName)
{
    Param (
    [Parameter(Mandatory=$true)]
    [string]$TenantName,

    [Parameter(Mandatory=$true)]
    [string]$AppName,

    [Parameter(Mandatory=$true)]
    [string]$FunctionName
    )
    
    # Check if App Registration exists
    $appReg = (az ad app list --display-name $appName)
    if ($null -ne $appReg -and $appReg.Count -gt 1) { 
        Write-Error "App $appName already exists, exiting"
        return $null;
    }

    # Create App Registration
    $uris = @("https://$functionName.azurewebsites.net/.auth/login/aad/callback", `
            "https://localhost:7071/api/swagger/oauth2-redirect.html", `
            "http://localhost:7071/api/swagger/oauth2-redirect.html", `
            "https://$functionName.azurewebsites.net/api/swagger/oauth2-redirect.html")
    
    $newAppReg = $(az ad app create --display-name $appName --web-redirect-uris $uris --enable-access-token-issuance --enable-id-token-issuance) | ConvertFrom-Json

    # Update App Registration with additional settings
    $appId = $newAppReg.AppId
    $result = $newAppReg.AppId
    $azAppObjectId = $newAppReg.Id
    $identifierUri = @("https://$appId.$tenantName.onmicrosoft.com")

    az ad app update --id $azAppObjectId --identifier-uris $identifierUri -o none

    # Create Service Principal and link it to newly created App Registration
    az ad sp create --id $azAppObjectId -o none

    # Create new scopes from file 'oath2-permissions'
    # //note the AZ CLI version 2.37 has some changes and this is workaround for now
    # see https://github.com/Azure/azure-cli/issues/23969
    # Note this works as is if the App is new and does not yet have any scopes, otherwise reset first

    $scopeGUID = [guid]::NewGuid()
    $scopeJSONHash = @{
        adminConsentDescription="Access to IoTEdgeDeploymentEngine"
        adminConsentDisplayName="Access to IoTEdgeDeploymentEngine" 
        id="$scopeGUID"
        isEnabled=$true
        type="User"
        userConsentDescription="Access to IoTEdgeDeploymentEngine"
        userConsentDisplayName="Access to IoTEdgeDeploymentEngine"
        value="user_impersonation"
    }

    $accesstoken = $(az account get-access-token --resource-type ms-graph | ConvertFrom-Json).accessToken
    $header = @{
        'Content-Type' = 'application/json'
        'Authorization' = 'Bearer ' + $accesstoken
    }
    $bodyAPIAccess = @{
        'api' = @{
            'oauth2PermissionScopes' = @($scopeJSONHash)
        }
    }  | ConvertTo-Json -Depth 3 -Compress

    $invoke = Invoke-RestMethod -Method Patch -Uri "https://graph.microsoft.com/v1.0/applications/$azAppObjectId" -Headers $header -Body $bodyAPIAccess

    return "$result"

}

Export-ModuleMember -Function RegisterApp