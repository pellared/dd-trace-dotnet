// <copyright file="ILogEventPropertyValue.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.Serilog
{
    /// <summary>
    /// A property associated with a <see cref="ILogEvent"/>.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ILogEventPropertyValue
    {
        /// <summary>
        /// Formats the value of the current instance using the specified format.
        /// </summary>
        /// <returns>
        /// The value of the current instance in the specified format.
        /// </returns>
        /// <param name="format">The format to use.-or- A null reference (Nothing in Visual Basic) to use
        /// the default format defined for the type of the <see cref="T:System.IFormattable"/> implementation. </param>
        /// <param name="formatProvider">The provider to use to format the value.-or- A null reference
        /// (Nothing in Visual Basic) to obtain the numeric format information from the current locale
        /// setting of the operating system. </param>
        string ToString(string format, IFormatProvider formatProvider);
    }
}
