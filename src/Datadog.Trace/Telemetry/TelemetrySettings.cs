// <copyright file="TelemetrySettings.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using Datadog.Trace.Configuration;

namespace Datadog.Trace.Telemetry
{
    internal class TelemetrySettings
    {
        public TelemetrySettings(IConfigurationSource source)
        {
            TelemetryEnabled = source?.GetBool(ConfigurationKeys.TelemetryEnabled) ??
                               // default value
                               true;

            var requestedTelemetryUri = source?.GetString(ConfigurationKeys.TelemetryUri);

            TelemetryUrl = !string.IsNullOrEmpty(requestedTelemetryUri) && Uri.TryCreate(requestedTelemetryUri, UriKind.Absolute, out var telemetryUri)
                               ? telemetryUri
                               : new Uri(TelemetryConstants.DefaultEndpoint);
        }

        /// <summary>
        /// Gets or sets a value indicating whether internal telemetry is enabled
        /// </summary>
        /// <seealso cref="ConfigurationKeys.TelemetryEnabled"/>
        public bool TelemetryEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the URL where telemetry should be sent
        /// </summary>
        /// <seealso cref="ConfigurationKeys.TelemetryUri"/>
        public Uri TelemetryUrl { get; set; }

        public static TelemetrySettings FromDefaultSources()
            => new TelemetrySettings(GlobalSettings.CreateDefaultConfigurationSource());
    }
}
