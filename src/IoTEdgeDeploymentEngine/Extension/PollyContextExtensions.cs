using System;
using Microsoft.Extensions.Logging;
using Polly;

namespace IoTEdgeDeploymentEngine.Extension
{
    /// <summary>
    /// Extensions for Polly context
    /// </summary>
    public static class PollyContextExtensions
    {
        private static readonly string LoggerKey = "ILogger";

        //Note see https://github.com/App-vNext/Polly/wiki/Polly-and-HttpClientFactory#configuring-httpclientfactory-policies-to-use-an-iloggert-from-the-call-site
        /// <summary>
        /// Extension method GetLogger
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static Context WithLogger<T>(this Context context, ILogger logger)
        {
            context[LoggerKey] = logger;
            return context;
        }

        /// <summary>
        /// Extension method to get the logger
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static ILogger GetLogger(this Context context)
        {
            if (context.TryGetValue(LoggerKey, out object logger))
            {
                return logger as ILogger;
            }

            return null;
        }

    }
}