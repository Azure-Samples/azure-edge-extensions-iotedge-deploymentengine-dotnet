Param(
    [Parameter(Mandatory=$true)]
    [string]$iotHubName,

    [Parameter(Mandatory=$true)]
    [int]
    $numberOfDevices,

    [Parameter(Mandatory=$false)]
    [string]
    $identityPrefix = "edgedevice",

    [Parameter(Mandatory=$true)]
    [string]$sourceLayeredManifestFile,

    [Parameter(Mandatory=$true)]
    [string]$destinationLayeredManifestDirectory

)

for ($i = 1; $i -le $numberOfDevices; $i++) {

    az iot hub device-identity create -n $iotHubName -d "$identityPrefix-$i" --ee  -o none
    az iot hub device-twin update -n $iotHubName -d "$identityPrefix-$i" --tags '{\"iiot\": true}' -o none
    Write-Host "Added device $identityPrefix-$i"
}

Write-Host "Generated $numberOfDevices in IoT Hub $iotHubName"

# Generate layered manifest with target condition per deviceId
$sourceJson = (Get-Content -Path $sourceLayeredManifestFile -Raw)

for ($i = 1; $i -le $numberOfDevices; $i++) {
    $deviceId = "$identityPrefix-$i"
    $newJson = $sourceJson.Replace('TO_REPLACE_DEVICE_ID', $deviceId)
    Set-Content -Path (Join-Path -Path "$destinationLayeredManifestDirectory" -ChildPath "$deviceId.json") -Value $newJson
    Write-Host "Generated file $deviceId.json"
}

Write-Host "Generated $numberOfDevices manifest files in folder $destinationLayeredManifestDirectory"

# End =========================


