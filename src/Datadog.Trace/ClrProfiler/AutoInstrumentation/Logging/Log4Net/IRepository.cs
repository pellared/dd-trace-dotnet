// <copyright file="IRepository.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.Log4Net
{
    /// <summary>
    /// Duck type for Repository
    /// </summary>
    public interface IRepository
    {
        /// <summary>
        /// Gets or sets a value indicating whether this repository has been configured
        /// </summary>
        public bool Configured { get; set; }
    }
}
