// <copyright file="IArray.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.Log4Net
{
    /// <summary>
    /// Duck type for Array
    /// </summary>
    public interface IArray
    {
        /// <summary>
        /// Gets the length of the array
        /// </summary>
        public int Length { get; }
    }
}
