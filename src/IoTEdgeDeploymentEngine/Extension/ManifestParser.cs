using System;
using System.Collections.Generic;
using System.Linq;
using IoTEdgeDeploymentEngine.Config;
using IoTEdgeDeploymentEngine.Util;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;

namespace IoTEdgeDeploymentEngine.Extension
{
	/// <summary>
	/// Collection of extension methods to parse manifest file
	/// </summary>
	public static class ManifestParser
	{
		private const string RouteKey = "route";

		/// <summary>
		/// Reads edgeAgents module specification
		/// </summary>
		/// <param name="edgeAgentModules">Modules content of edgeAgent specification</param>
		public static List<EdgeModuleSpecification> GetEdgeAgentModulesSpec(
			this IEnumerable<KeyValuePair<string, object>> edgeAgentModules)
		{
			var modulesSpec = new List<EdgeModuleSpecification>();
			foreach (var edgeAgentModule in edgeAgentModules)
			{
				var moduleName = string.Empty;
				DeploymentManifest.SystemModuleSpecification mod = null;
				if (edgeAgentModule.Key != "properties.desired")
				{
					var edgeAgentJson = edgeAgentModule.Value.ToString();
					mod = JsonConvert
						.DeserializeObject<DeploymentManifest.SystemModuleSpecification>(edgeAgentJson);
					moduleName = edgeAgentModule.Key.Replace("properties.desired.modules.", "");
				}
				else
				{
					dynamic edgeAgentValue = edgeAgentModule.Value;
					foreach (var moduleSpecification in edgeAgentValue["modules"])
					{
						mod = JsonConvert
							.DeserializeObject<DeploymentManifest.SystemModuleSpecification>(moduleSpecification.Value
								.ToString());
						moduleName = moduleSpecification.Name?.ToString();
					}
				}

				if (null == mod)
					continue;

				var envVars = mod?.Env?.Select(e => new EnvironmentVariable(e.Key, e.Value.Value)).ToList();

				if (modulesSpec.Any(m => m.Name == moduleName))
					continue;
				
				modulesSpec.Add(new EdgeModuleSpecification(moduleName,
					mod?.Settings?.Image, "1.0",
					string.IsNullOrEmpty(mod?.RestartPolicy)
						? RestartPolicy.Always
						: Enum.Parse<RestartPolicy>(mod?.RestartPolicy, true),
					mod?.Settings?.CreateOptions == null
						? string.Empty
						: mod.Settings.CreateOptions,
					string.IsNullOrEmpty(mod?.Status)
						? ModuleStatus.Running
						: Enum.Parse<ModuleStatus>(mod?.Status, true),
					envVars));
			}

			return modulesSpec;
		}

		/// <summary>
		/// Reads edgeAgents system module specification
		/// </summary>
		/// <param name="edgeAgentModules">Modules content of edgeAgent Specification</param>
		/// <returns></returns>
		public static List<EdgeModuleSpecification> GetEdgeAgentSystemModulesSpec(
			this IEnumerable<KeyValuePair<string, object>> edgeAgentModules)
		{
			var systemModulesSpec = new List<EdgeModuleSpecification>();
			foreach (var edgeAgentModule in edgeAgentModules)
			{
				if (edgeAgentModule.Key != "properties.desired")
					continue;

				dynamic edgeAgentValue = edgeAgentModule.Value;
				foreach (var moduleSpecification in edgeAgentValue["systemModules"])
				{
					DeploymentManifest.SystemModuleSpecification mod = JsonConvert
						.DeserializeObject<DeploymentManifest.SystemModuleSpecification>(moduleSpecification.Value
							.ToString());
					var envVars = mod?.Env?.Select(e => new EnvironmentVariable(e.Key, e.Value.Value)).ToList();
					systemModulesSpec.Add(new EdgeModuleSpecification(moduleSpecification.Name?.ToString(),
						mod?.Settings?.Image, "1.0",
						string.IsNullOrEmpty(mod?.RestartPolicy)
							? RestartPolicy.Always
							: Enum.Parse<RestartPolicy>(mod?.RestartPolicy, true),
						mod?.Settings?.CreateOptions == null
							? string.Empty
							: mod.Settings.CreateOptions,
						string.IsNullOrEmpty(mod?.Status)
							? ModuleStatus.Running
							: Enum.Parse<ModuleStatus>(mod?.Status, true),
						envVars));
				}
			}

			return systemModulesSpec;
		}

		/// <summary>
		/// Reads all routes from edgeHub modules and returns them in a list.
		/// </summary>
		/// <param name="edgeHubProps">Content of edgeHub properties</param>
		/// <returns></returns>
		public static List<Route> GetEdgeHubRoutes(this IEnumerable<KeyValuePair<string, object>> edgeHubProps)
		{
			var flattenedRoutes = new List<Route>();
			foreach (var routeProp in edgeHubProps)
			{
				dynamic routePropValue = routeProp.Value;
				if (routeProp.Key != "properties.desired")
				{
					var key = routeProp.Key.Replace("properties.desired.routes.", "");
					foreach (var route in routePropValue)
					{
						if (!flattenedRoutes.Any(f => f.Name == key))
							flattenedRoutes.Add(new Route(key, route.Value?.ToString()));
					}
				}
				else
				{
					var desiredProps =
						JsonConvert.DeserializeObject<DeploymentManifest.PropertiesDesiredEdgeHub>(
							routePropValue.ToString());

					if (desiredProps?.Routes == null) continue;
					foreach (var route in desiredProps?.Routes)
					{
						if (!flattenedRoutes.Any(f => f.Name == route.Key))
							flattenedRoutes.Add(new Route(route.Key, route.Value[RouteKey]));
					}
				}
			}

			return flattenedRoutes;
		}

		/// <summary>
		/// Reads edgeAgent module settings and returns RegistryCredentials
		/// </summary>
		/// <param name="edgeAgentModules">Modules content of edgeAgent specification</param>
		/// <returns></returns>
		public static IEnumerable<RegistryCredential> GetEdgeAgentRegCreds(
			this IEnumerable<KeyValuePair<string, object>> edgeAgentModules)
		{
			foreach (var edgeAgentModule in edgeAgentModules)
			{
				if (edgeAgentModule.Key != "properties.desired") continue;

				dynamic edgeAgentValue = edgeAgentModule.Value;
				var edgeAgentRuntimeJson = edgeAgentValue["runtime"]?["settings"]?["registryCredentials"]?.ToString();
				if (string.IsNullOrEmpty(edgeAgentRuntimeJson))
					continue;

				Dictionary<string, DeploymentManifest.Store> runtime =
					JsonConvert.DeserializeObject<Dictionary<string, DeploymentManifest.Store>>(
						edgeAgentRuntimeJson);
				foreach (var regCred in runtime)
				{
					yield return new RegistryCredential(regCred.Key,
						regCred.Value?.Address, regCred.Value?.Username,
						regCred.Value?.Password);
				}
			}
		}

		/// <summary>
		/// Reads edgeAgent module settings and returns SchemaVersion
		/// </summary>
		/// <param name="edgeAgentModules">Modules content of edgeAgent specification</param>
		/// <returns></returns>
		public static string GetEdgeAgentSchemaVersion(this IEnumerable<KeyValuePair<string, object>> edgeAgentModules)
		{
			foreach (var edgeAgentModule in edgeAgentModules)
			{
				if (edgeAgentModule.Key != "properties.desired") continue;

				dynamic edgeAgentValue = edgeAgentModule.Value;
				var schemaVersion = edgeAgentValue["schemaVersion"]?.ToString();
				if (!string.IsNullOrEmpty(schemaVersion))
					return schemaVersion;
			}

			return string.Empty;
		}
		
		/// <summary>
		/// Selects first item per key in a sequence and returns those distinct KeyValuePairs
		/// </summary>
		/// <param name="source">Source enumeration</param>
		/// <returns></returns>
		public static IEnumerable<KeyValuePair<string, object>> FirstPerKey(
			this IEnumerable<KeyValuePair<string, object>> source)
		{
			var retVal = new List<KeyValuePair<string, object>>();

			foreach (var ordered in source)
			{
				if (retVal.Any(r => r.Key == ordered.Key))
					continue;
				retVal.Add(ordered);
			}

			return retVal;
		}
		
		/// <summary>
		/// Selects first item per key in a sequence and returns those distinct KeyValuePairs
		/// </summary>
		/// <param name="source">Source enumeration</param>
		/// <returns></returns>
		public static IEnumerable<KeyValuePair<string, IDictionary<string, object>>> FirstPerKey(
			this IEnumerable<KeyValuePair<string, IDictionary<string, object>>> source)
		{
			var retVal = new List<KeyValuePair<string, IDictionary<string, object>>>();

			foreach (var ordered in source)
			{
				if (retVal.Any(r => r.Key == ordered.Key))
					continue;
				retVal.Add(ordered);
			}

			return retVal;
		}
	}
}