//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the GeneratePackageVersions tool. To safely
//     modify this file, edit PackageVersionsGeneratorDefinitions.json and
//     re-run the GeneratePackageVersions project in Visual Studio. See the
//     launchSettings.json for the project if you would like to run the tool
//     with the correct arguments outside of Visual Studio.

//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated. 
// </auto-generated>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    [SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1516:Elements must be separated by blank line", Justification = "This is an auto-generated file.")]
    public class PackageVersions
    {
#if TEST_ALL_MINOR_PACKAGE_VERSIONS
        public static readonly bool IsAllMinorPackageVersions = true;
#else
        public static readonly bool IsAllMinorPackageVersions = false;
#endif

        public static IEnumerable<object[]> AwsSqs => IsAllMinorPackageVersions ? PackageVersionsLatestMinors.AwsSqs : PackageVersionsLatestMajors.AwsSqs;

        public static IEnumerable<object[]> MongoDB => IsAllMinorPackageVersions ? PackageVersionsLatestMinors.MongoDB : PackageVersionsLatestMajors.MongoDB;

        public static IEnumerable<object[]> ElasticSearch7 => IsAllMinorPackageVersions ? PackageVersionsLatestMinors.ElasticSearch7 : PackageVersionsLatestMajors.ElasticSearch7;

        public static IEnumerable<object[]> ElasticSearch6 => IsAllMinorPackageVersions ? PackageVersionsLatestMinors.ElasticSearch6 : PackageVersionsLatestMajors.ElasticSearch6;

        public static IEnumerable<object[]> ElasticSearch5 => IsAllMinorPackageVersions ? PackageVersionsLatestMinors.ElasticSearch5 : PackageVersionsLatestMajors.ElasticSearch5;

        public static IEnumerable<object[]> Npgsql => IsAllMinorPackageVersions ? PackageVersionsLatestMinors.Npgsql : PackageVersionsLatestMajors.Npgsql;

        public static IEnumerable<object[]> RabbitMQ => IsAllMinorPackageVersions ? PackageVersionsLatestMinors.RabbitMQ : PackageVersionsLatestMajors.RabbitMQ;

        public static IEnumerable<object[]> SystemDataSqlClient => IsAllMinorPackageVersions ? PackageVersionsLatestMinors.SystemDataSqlClient : PackageVersionsLatestMajors.SystemDataSqlClient;

        public static IEnumerable<object[]> MicrosoftDataSqlClient => IsAllMinorPackageVersions ? PackageVersionsLatestMinors.MicrosoftDataSqlClient : PackageVersionsLatestMajors.MicrosoftDataSqlClient;

        public static IEnumerable<object[]> StackExchangeRedis => IsAllMinorPackageVersions ? PackageVersionsLatestMinors.StackExchangeRedis : PackageVersionsLatestMajors.StackExchangeRedis;

        public static IEnumerable<object[]> ServiceStackRedis => IsAllMinorPackageVersions ? PackageVersionsLatestMinors.ServiceStackRedis : PackageVersionsLatestMajors.ServiceStackRedis;

        public static IEnumerable<object[]> MySqlData => IsAllMinorPackageVersions ? PackageVersionsLatestMinors.MySqlData : PackageVersionsLatestMajors.MySqlData;

        public static IEnumerable<object[]> MySqlConnector => IsAllMinorPackageVersions ? PackageVersionsLatestMinors.MySqlConnector : PackageVersionsLatestMajors.MySqlConnector;

        public static IEnumerable<object[]> MicrosoftDataSqlite => IsAllMinorPackageVersions ? PackageVersionsLatestMinors.MicrosoftDataSqlite : PackageVersionsLatestMajors.MicrosoftDataSqlite;

        public static IEnumerable<object[]> XUnit => IsAllMinorPackageVersions ? PackageVersionsLatestMinors.XUnit : PackageVersionsLatestMajors.XUnit;

        public static IEnumerable<object[]> NUnit => IsAllMinorPackageVersions ? PackageVersionsLatestMinors.NUnit : PackageVersionsLatestMajors.NUnit;

        public static IEnumerable<object[]> MSTest => IsAllMinorPackageVersions ? PackageVersionsLatestMinors.MSTest : PackageVersionsLatestMajors.MSTest;

        public static IEnumerable<object[]> Kafka => IsAllMinorPackageVersions ? PackageVersionsLatestMinors.Kafka : PackageVersionsLatestMajors.Kafka;

        public static IEnumerable<object[]> CosmosDb => IsAllMinorPackageVersions ? PackageVersionsLatestMinors.CosmosDb : PackageVersionsLatestMajors.CosmosDb;

        public static IEnumerable<object[]> Serilog => IsAllMinorPackageVersions ? PackageVersionsLatestMinors.Serilog : PackageVersionsLatestMajors.Serilog;

        public static IEnumerable<object[]> NLog => IsAllMinorPackageVersions ? PackageVersionsLatestMinors.NLog : PackageVersionsLatestMajors.NLog;

        public static IEnumerable<object[]> log4net => IsAllMinorPackageVersions ? PackageVersionsLatestMinors.log4net : PackageVersionsLatestMajors.log4net;

        public static IEnumerable<object[]> Aerospike => IsAllMinorPackageVersions ? PackageVersionsLatestMinors.Aerospike : PackageVersionsLatestMajors.Aerospike;
    }
}
