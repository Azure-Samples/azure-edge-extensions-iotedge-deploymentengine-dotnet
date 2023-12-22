# Azure DevOps Pipelines: setting up a pipeline with IoTEdgeDeployment Engine to trigger deployments

## Contents

- [Introduction](#introduction)
- [Step-by-step setting up the pipeline](#step-by-step)

## Introduction

This document provides step-by-step instructions for setting up an Azure DevOps pipeline that uses a [.NET Console application](../src/IoTEdgeDeploymentTester/)  to apply all (base and layered) deployments to matching devices based on manifest files. The goal is to automate the merging and application of manifests through a CI/CD pipeline, allowing for easy deployment and updates of IoT devices in a production environment with the latest code changes committed to a Git repository.

To get started, you'll learn how to create a set of devices in your IoT Hub and corresponding layered manifests for testing.

You'll also prepare variables and secrets in Azure Key Vault, which will be used to replace secrets in manifest files. Finally, you'll assign RBAC permissions for the Azure DevOps Service Principal, which is necessary for the pipeline to execute in Azure. By following these steps, you'll be able to set up an automated deployment process for your IoT devices.

## Pre-requisites

- Azure DevOps project
- Azure CLI
- PowerShell core on Linux/Mac/Windows or PowerShell on Windows
- Required resources in Azure: either having run the developer setup described in [Readme](../README.md), or by having the following resources available:
    - Azure IoT Hub with edge devices provisioned
    - Azure Key Vault for storing secrets used by the pipeline: optional, you could replace this with usage of variables (groups) in Azure DevOps

## Step-by-step

This setup assumes the `.json` manifest files required for Automatic and Layered deployments are stored within the a Git repo that is accessible by the CI/CD pipeline. 
You have other options, for example generating the base/layered manifest files within your CI/CD pipelines which will slightly change the steps defined below. Use the below as an example of how this could work but adapt to best fit yours needs.

### Initial setup of source manifest files

To create a set of devices in your IoT Hub and corresponding layered manifests for testing you can run the PowerShell script `deployment/helpers/createDevices.ps1`.

1. Ensure you have a `/manifestsroot` folder within your repo. You can call this what you want.
2. Create at least one base manifest file, or copy the provided ones from the folder [../manifests/AutomaticDeployment](../manifests/AutomaticDeployment/).
3. First create a source **layered** deployment `.json` file. You can use the provided [`baselayersample.json`](baselayersample.json) in this folder and leave it there. This sample file has a target condition that is specific to a single device so you can test the Engine to have a unique layer per device. You can see the setup of the `targetCondition` used:

  ```json
  "targetCondition": "deviceId = 'TO_REPLACE_DEVICE_ID'",

  ```

4. Leave the `TO_REPLACE_DEVICE_ID` as is, since the `deployment/helpers/createDevices.ps1` script will replace this in the file contents when executed. 
5. Run the device and file generation script in PowerShell:

  ```powershell
  ./createDevices.ps1 -iotHubName <iothubname> -numberOfDevices 2 -identityPrefix "<devicenameprefix>" -sourceLayeredManifestFile ./baselayersample.json -destinationLayeredManifestDirectory "<full path_to_CICDrepo>/maniestsroot/LayeredDeployment/"
  ```

You can commit the generated files to your code repo. Here's an example of a base repo that is required for the pipeline to run:

```
─manifestsroot
    ├───AutomaticDeployment
    │       automaticbase.json
    └───LayeredDeployment
            edgedevice-1.json
            edgedevice-2.json
            edgedevice-3.json
            edgedevice-4.json
            edgedevice-5.json
            edgedevice-6.json
            edgedevice-7.json
            edgedevice-8.json
            edgedevice-9.json
```

### Prepare variable and secrets in Azure Key Vault.

Add the following variables to the Azure Key Vault you will be using for storing secrets used by the pipeline:
  - `IOTHUBHOSTNAME` = IoT Hub hostname in the form of `xxx.azure-devices.net`
  - `KEYVAULTURI` = Key Vault URI including `https://` prefix. This could be another Key Vault account than the one you are adding your secrets to, as it will be used for replacing secrets in manifest files. This is optional and only needed if you are replacing secret placeholders in the manifest `json` files.

### Assigning RBAC permissions for the Azure DevOps Service Principal

The pipeline will execute in Azure under a Service Principal account that will require access to IoT Hub (3 roles) and Azure Key Vault Secrets Officer.

1. In your Azure DevOps project, add or identify an [Azure Service Connection](https://learn.microsoft.com/en-us/azure/devops/pipelines/library/service-endpoints?view=azure-devops&tabs=yaml) setup to connect to your Azure chosen subscription. 
2. Identify the Service Principal object used for this connection. Normally you can click the **Manage Service Principal** link to take you directly to Azure Active Directory.
3. Copy the AppId of this Service Principal.
4. Execute the Role assignment script below in a PowerShell terminal:

```powershell
$principalId = <AppId of the service principal>
$keyVaultName = <Key Vault resource name>
$iotHubName = <IoT Hub resource name>
$subscriptionId - <Subscription ID>

az role assignment create --assignee $principalId `
    --role "Key Vault Secrets Officer" `
    --scope "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.KeyVault/vaults/$keyVaultName" -o none

az role assignment create --assignee $principalId `
    --role "IoT Hub Data Contributor" `
    --scope "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.Devices/IotHubs/$iotHubName" -o none

az role assignment create --assignee $principalId `
    --role "IoT Hub Registry Contributor" `
    --scope "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.Devices/IotHubs/$iotHubName" -o none

az role assignment create --assignee $principalId `
    --role "IoT Hub Twin Contributor" `
    --scope "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.Devices/IotHubs/$iotHubName" -o none
```

### Setting up the Azure DevOps Pipeline

1. Ensure you have an Azure DevOps project or create a new one.
2. Create or ensure you have an Azure Service Connection. This will be used to authenticate to the Azure IoT Hub and Key Vault resources.
3. Create a new pipeline and enter the following YAML, replacing the values in `<>` with your specific values.

```yaml
trigger:
- main

pool:
  vmImage: ubuntu-latest

variables:
- name: buildConfiguration
  value: 'Release'
- name: ROOT_MANIFESTS_FOLDER
  value: $(Build.SourcesDirectory)/manifestsroot
- name: CONTINUE_ON_ERROR
  value: true

steps:

- task: AzureKeyVault@2
  inputs:
    azureSubscription: '<REPLACE: YOUR AZURE SERVICECONNECTION>'
    KeyVaultName: '<REPLACE: NAME OF THE KEY VAULT>'
    SecretsFilter: 'IOTHUBHOSTNAME,KEYVAULTURI'
    RunAsPreJob: true


- task: CmdLine@2
  inputs:
    script: |
      echo 'Cloning engine repo'
      git clone https://github.com/Azure-Samples/iot-edge-deployment-engine.git --progress --branch main --single-branch --depth=1 $(Build.SourcesDirectory)/engine

- task: CmdLine@2
  inputs:
    script: |
      echo "##vso[task.setvariable variable=IOTHUB_HOSTNAME]$(IOTHUBHOSTNAME)"
      echo "##vso[task.setvariable variable=KEYVAULT_URI]$(KEYVAULTURI)"

- task: AzureCLI@2
  inputs:
    azureSubscription: '<REPLACE: YOUR AZURE SERVICECONNECTION>'
    scriptLocation: 'inlineScript'
    scriptType: bash
    inlineScript: 'dotnet run --project "$(Build.SourcesDirectory)/engine/src/IoTEdgeDeploymentTester/IoTEdgeDeploymentTester.csproj" --configuration Debug'

```
4. Ensure the path to the `manifestsroot` defined by the variable `ROOT_MANIFESTS_FOLDER` is correct in the pipeline. In the current care the Azure DevOps project only has one repo. If you have multiple you will need to correctly address it, see [Build variables, specifically `Build.SourcesDirectory`](https://learn.microsoft.com/en-us/azure/devops/pipelines/build/variables?view=azure-devops&tabs=yaml#build-variables-devops-services).
5. Decide how you want the .NET Console task to behave in case of failure: set the variable `CONTINUE_ON_ERROR`.
  - `true`: in this case the Console app might return an error but still continue as the task itself catches the error. 
  - `false`: the task will exit with an error and the Pipeline will return as failed.
6. Run the pipeline and validate each step executed successfully.

## Clean-up resources

Deleting the Azure Pipeline will disable IoT Edge deployments using this pipeline. Cleaning up the directory with manifests will also ensure nothing gets applied to IoT Edge devices in the selected IoT Hub. 
For deleting Azure resources created with the developer setup please see the [Readme](../README.md) file in the root of this repo.