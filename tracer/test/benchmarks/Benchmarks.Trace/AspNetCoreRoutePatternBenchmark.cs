#if NETCOREAPP
using System.Collections.Generic;
using System.Net.Http;
using BenchmarkDotNet.Attributes;
using Datadog.Trace.DiagnosticListeners;
using Datadog.Trace.DuckTyping;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.Template;
using RoutePattern = Datadog.Trace.DiagnosticListeners.RoutePattern;

namespace Benchmarks.Trace
{
    [MemoryDiagnoser]
    public class AspNetCoreRoutePatternBenchmark
    {
        private static readonly HttpClient Client;
        private static readonly RoutePattern _routePattern;
        private static readonly RouteValueDictionary _routeValues;
        private static readonly IDictionary<string, string> _routeValueDictionary;
        private static readonly string _area;
        private static readonly string _controller;
        private static readonly string _action;
        private static readonly RouteTemplate _routeTemplate;

        static AspNetCoreRoutePatternBenchmark()
        {
            var routePattern = RoutePatternFactory.Parse("{controller=Home}/{action=Index}{id?}");
            _routeTemplate = new RouteTemplate(routePattern);
            _routePattern = routePattern.DuckCast<RoutePattern>();
            _routeValues = new RouteValueDictionary { { "controller", "Home" }, { "action", "Index" }, };
            _routeValueDictionary = new Dictionary<string, string> { { "controller", "Home" }, { "action", "Index" }, };
            _controller = "Home";
            _action = "Index";
            _area = null;
        }

        [Benchmark]
        public string OriginalSimplifyRoutePattern()
        {
            return AspNetCoreDiagnosticObserver.SimplifyRoutePattern(_routePattern, _routeValues, _area, _controller, _action);
        }

        [Benchmark]
        public string TryDuckCastSimplifyRoutePattern()
        {
            return AspNetCoreDiagnosticObserver.SimplifyRoutePatternTryDuckCast(_routePattern, _routeValues, _area, _controller, _action);
        }

        [Benchmark]
        public string OriginalSimplifyRouteTemplate()
        {
            return AspNetCoreDiagnosticObserver.SimplifyRoutePattern(_routeTemplate, _routeValueDictionary, _area, _controller, _action);
        }
    }
}
#endif
