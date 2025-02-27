﻿// <copyright file="Data.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
// Based on https://github.com/dotnet/performance/blob/ef497aa104ae7abe709c71fbb137230bf5be25e9/src/tools/ResultsComparer

using System.Collections.Generic;
using System.Linq;
using CsvHelper.Configuration.Attributes;
using Perfolizer.Mathematics.SignificanceTesting;

namespace BenchmarkComparison
{
    public class ChronometerFrequency
    {
        public int Hertz { get; set; }
    }

    public class HostEnvironmentInfo
    {
        public string BenchmarkDotNetCaption { get; set; }
        public string BenchmarkDotNetVersion { get; set; }
        public string OsVersion { get; set; }
        public string ProcessorName { get; set; }
        public int? PhysicalProcessorCount { get; set; }
        public int? PhysicalCoreCount { get; set; }
        public int? LogicalCoreCount { get; set; }
        public string RuntimeVersion { get; set; }
        public string Architecture { get; set; }
        public bool? HasAttachedDebugger { get; set; }
        public bool? HasRyuJit { get; set; }
        public string Configuration { get; set; }
        public string JitModules { get; set; }
        public string DotNetCliVersion { get; set; }
        public ChronometerFrequency ChronometerFrequency { get; set; }
        public string HardwareTimerKind { get; set; }
    }

    public class ConfidenceInterval
    {
        public int N { get; set; }
        public double Mean { get; set; }
        public double StandardError { get; set; }
        public int Level { get; set; }
        public double Margin { get; set; }
        public double Lower { get; set; }
        public double Upper { get; set; }
    }

    public class Percentiles
    {
        public double P0 { get; set; }
        public double P25 { get; set; }
        public double P50 { get; set; }
        public double P67 { get; set; }
        public double P80 { get; set; }
        public double P85 { get; set; }
        public double P90 { get; set; }
        public double P95 { get; set; }
        public double P100 { get; set; }
    }

    public class Statistics
    {
        public int N { get; set; }
        public double Min { get; set; }
        public double LowerFence { get; set; }
        public double Q1 { get; set; }
        public double Median { get; set; }
        public double Mean { get; set; }
        public double Q3 { get; set; }
        public double UpperFence { get; set; }
        public double Max { get; set; }
        public double InterquartileRange { get; set; }
        public List<double> LowerOutliers { get; set; }
        public List<double> UpperOutliers { get; set; }
        public List<double> AllOutliers { get; set; }
        public double StandardError { get; set; }
        public double Variance { get; set; }
        public double StandardDeviation { get; set; }
        public double? Skewness { get; set; }
        public double? Kurtosis { get; set; }
        public ConfidenceInterval ConfidenceInterval { get; set; }
        public Percentiles Percentiles { get; set; }
    }

    public class Memory
    {
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }
        public long TotalOperations { get; set; }
        public long BytesAllocatedPerOperation { get; set; }
    }

    public class Measurement
    {
        public string IterationStage { get; set; }
        public int LaunchIndex { get; set; }
        public int IterationIndex { get; set; }
        public long Operations { get; set; }
        public double Nanoseconds { get; set; }
    }

    public class Benchmark
    {
        public string DisplayInfo { get; set; }
        public object Namespace { get; set; }
        public string Type { get; set; }
        public string Method { get; set; }
        public string MethodTitle { get; set; }
        public string Parameters { get; set; }
        public string FullName { get; set; }
        public Statistics Statistics { get; set; }
        public Memory Memory { get; set; }
        public List<Measurement> Measurements { get; set; }

        /// <summary>
        /// this method was not auto-generated by a tool, it was added manually
        /// </summary>
        /// <returns>an array of the actual workload results (not warmup, not pilot)</returns>
        internal double[] GetOriginalValues()
            => Measurements
              .Where(measurement => measurement.IterationStage == "Result")
              .Select(measurement => measurement.Nanoseconds / measurement.Operations)
              .ToArray();
    }

    public class BdnResult
    {
        public string FileName { get; set; }
        public string Title { get; set; }
        public HostEnvironmentInfo HostEnvironmentInfo { get; set; }
        public List<Benchmark> Benchmarks { get; set; }
    }

    public record MatchedSummary(
        string BenchmarkName,
        List<BdnBenchmarkSummary> BaseSummary,
        List<BdnBenchmarkSummary> DiffSummary,
        List<BenchmarkComparison> Comparisons,
        List<AllocationComparison> AllocationComparisons)
    {
        public EquivalenceTestConclusion Conclusion
        {
            get
            {
                if (Comparisons.Any(x => x.Conclusion == EquivalenceTestConclusion.Slower))
                {
                    return EquivalenceTestConclusion.Slower;
                }
                else if (Comparisons.Any(x => x.Conclusion == EquivalenceTestConclusion.Faster))
                {
                    return EquivalenceTestConclusion.Faster;
                }
                else if (Comparisons.All(x => x.Conclusion == EquivalenceTestConclusion.Same))
                {
                    return EquivalenceTestConclusion.Same;
                }
                else
                {
                    return EquivalenceTestConclusion.Unknown;
                }
            }
        }

        public AllocationConclusion AllocationConclusion
        {
            get
            {
                if (AllocationComparisons.Any(x => x.Conclusion == AllocationConclusion.MoreAllocations))
                {
                    return AllocationConclusion.MoreAllocations;
                }
                else if (AllocationComparisons.Any(x => x.Conclusion == AllocationConclusion.FewerAllocations))
                {
                    return AllocationConclusion.FewerAllocations;
                }
                else if (AllocationComparisons.All(x => x.Conclusion == AllocationConclusion.Same))
                {
                    return AllocationConclusion.Same;
                }
                else
                {
                    return AllocationConclusion.Unknown;
                }
            }
        }
    }

    public record BenchmarkComparison(
        string Id,
        Benchmark BaseResult,
        Benchmark DiffResult,
        EquivalenceTestConclusion Conclusion);

    public record AllocationComparison(
        string Id,
        BdnBenchmarkSummary BaseResult,
        BdnBenchmarkSummary DiffResult,
        AllocationConclusion Conclusion);

    public record BdnRunSummary(string FileName, List<BdnBenchmarkSummary> Results);

    public class BdnBenchmarkSummary
    {
        public string Method { get; set; }
        public string Job { get; set; }
        public string Runtime { get; set; }
        public string Toolchain { get; set; }
        public string IterationTime { get; set; }
        public string Mean { get; set; }
        public string Error { get; set; }
        public string StdDev { get; set; }
        public string Ratio { get; set; }

        [Name("Gen 0")]
        public string Gen0 { get; set; }

        [Name("Gen 1")]
        public string Gen1 { get; set; }

        [Name("Gen 2")]
        public string Gen2 { get; set; }

        public string Allocated { get; set; }
    }

    public enum AllocationConclusion
    {
        Unknown = 0,
        Same = 1,
        MoreAllocations = 2,
        FewerAllocations = 3,
    }
}
