// <copyright file="IRequestHeaders.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if NETFRAMEWORK
using System.Collections.Generic;
using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.Integrations.AspNet
{
    /// <summary>
    /// RequestHeaders interface for ducktyping
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRequestHeaders
    {
        /// <summary>
        /// Gets the host from the HTTP request
        /// </summary>
        string Host { get; }

        /// <summary>
        /// Try get values from the headers
        /// </summary>
        /// <param name="name">Name of the header</param>
        /// <param name="values">Values of the header in the request</param>
        /// <returns>true if the header was found; otherwise, false</returns>
        bool TryGetValues(string name, out IEnumerable<string> values);

        /// <summary>
        /// Removes a header from the request
        /// </summary>
        /// <param name="name">Name of the header</param>
        /// <returns>true if the header was removed; otherwise, false.</returns>
        bool Remove(string name);

        /// <summary>
        /// Adds a header to the request
        /// </summary>
        /// <param name="name">Name of the header</param>
        /// <param name="value">Value of the header</param>
        void Add(string name, string value);
    }
}
#endif
