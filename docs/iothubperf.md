# IoT Edge Deployment Engine and IoT Hub: optimizing performance

## Contents

- [Introduction](#introduction)
- [IoT Hub queries](#iot-hub-queries)
- [Cost](#cost)
- [Background and parallel processing](#background-and-parallel-processing)

## Introduction

For most customers, having the Engine run and apply a set of deployments in a span of 2 minutes versus a span of 8 minutes through a CI/CD pipeline will not really matter. The consolidation and reconciliation of deployment manifests is typically something that needs to be done upon provisioning new devices, or more often, upon changing device Tags or updating the file based layered deployments. 

There has been some work done on testing the speed at which deployments will apply to the IoT Edge devices in IoT Hub. The current implementation of the IoT Edge Deployment Engine runs asynchronously where possible but also requires some of the consolidation to happen almost synchronously and in sequence.

The engine itself builds up single deployments per device, and the work done in memory is fast. However, calls to the IoT Hub have their operation rate limits which we need to take into account. There are two types of calls that are relevant for optimizing speed:

1. Queries to retrieve the devices matching every condition in the deployment files.
2. Twin update calls to apply configurations per device.

Both of these calls are [documented](https://learn.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-quotas-throttling) as follows:

| Throttle | Free, B1, and S1 | B2 and S2 | B3 and S3 | 
| -------- | ------- | ------- | ------- |
| Queries | 20/min/unit | 20/min/unit | 1,000/min/unit |
| Twin updates (device and module)<sup>1</sup> | 50/sec | Higher of 50/sec or 5/sec/unit | 250/sec/unit |

### IoT Hub queries

A query is run for every deployment (base or layered) file found in the manifests folder. The field `targetCondition` is triggering a call to IoT Hub query for every single file.

Queries' rate limits have the most impact within the engine's flow. With a typical 20 operations/minute/per IoT Hub unit, each call literally adds a second or more latency to each query that needs to run. This is the single most impactful point to consider improving when speed of execution is of importance.

Test 1: 100 files, 100 devices, **1x** S1 SKU IoT Hub unit

In this test scenario with 100 different target conditions, each having with 1 matching device per layer deployment file, we could see a run of about 5 minutes to process all deployments.

Test 2: 100 files, 100 devices, **2x** S1 SKU IoT Hub unit

Increasing the number of units of IoT Hub, even in S1 SKU will have an impact on the rate at which queries can be run and results returned. We could see a complete run execute in about 3 minutes versus the 5 minutes with a single S1 unit.

## Cost

Increasing IoT Hub units will have a cost impact. However, if speed at which the reconciliation applies is important this is the single most efficient way to improve execution times.
Note that increasing or decreasing the number of units has a cost per day, so increasing it just for an hour a day during engine execution will not have a lower cost than increasing it for the full 24 hours.

Assuming an average cost of $25/per month per S1 unit, doubling this to 2 units will double your monthly fee for the IoT Hub resource.

## Background and parallel processing

We have also evaluated the advantages to speed of execution with a background task and executing queries in parallel.

However, due to the rate limits explained above, this option was not returning any significant wins because the parallel processing would still be rate limited.
As such, our recommendation is to look at the optimization of the number of units of the IoT Hub before refactoring code.




