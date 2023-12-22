# Abstracted Logic

## Contents

- [Inversion-of-Control and Dependency Injection](#inversion-of-control-and-dependency-injection)
- [Base Logic](#base-logic)
- [Own Derived Implementation](#own-derived-implementation)
- [Register Logic](#register-logic)

## Inversion-of-Control and Dependency Injection

This solution heavily relies on the loose coupling design concept of inversion-of-control (IoC) and the dependency injection (DI) pattern. As a result, different implementations of the same interface are interchangeable.

## Base Logic

The deployment engine library contains an implementation that matches the behavior of applying deployment in the IoT Hub. The code is available in class [ModuleLogic][def].

## Own Derived Implementation

Own logic can be implementated in a derived class by overriding the virtual methods of class ModuleLogic, see sample at [SampleModuleLogic][def1].

## Register Logic

The host using the Deployment Engine library can register the business logic implementation of choice which will be resolved via constructor injection in the IoTEdgeDeploymentBuilder class, see [IoTEdgeDeploymentTester][def2].

```csharp
...
.AddSingleton<IModuleLogic, ModuleLogic>()
...
```

[def]: /src/IoTEdgeDeploymentEngine/Logic/ModuleLogic.cs
[def1]: /src/IoTEdgeDeploymentEngine/Logic/SampleModuleLogic.cs
[def2]: /src/IoTEdgeDeploymentTester/Program.cs
