// <copyright file="HostMetadata.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;

namespace Datadog.Trace.PlatformHelpers
{
    internal static class HostMetadata
    {
        private static readonly Lazy<string> Host = new(GetHostInternal);

        /// <summary>
        /// Gets the name of the host on which the code is running
        /// Returns <c>null</c> if the host name can not be determined
        /// </summary>
        /// <returns>The container id or <c>null</c>.</returns>
        public static string GetHost() => Host.Value;

        private static string GetHostInternal()
        {
            try
            {
                var host = Environment.MachineName;
                if (!string.IsNullOrEmpty(host))
                {
                    return host;
                }

                return Environment.GetEnvironmentVariable("COMPUTERNAME");
            }
            catch (InvalidOperationException)
            {
            }
            catch (System.Security.SecurityException)
            {
                // We may get a security exception looking up the machine name
                // You must have Unrestricted EnvironmentPermission to access resource
            }

            return null;
        }
    }
}
