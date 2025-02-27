using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.IO;
using System.Linq;
using System.Collections.Generic;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.MSBuild;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;

partial class Build
{
    Target CompileProfilerManagedSrc => _ => _
        .Unlisted()
        .Description("Compiles the managed code in the src directory")
        .After(CreateRequiredDirectories)
        .After(Restore)
        .Executes(() =>
        {
            List<string> projects = new();
            projects.Add(SharedDirectory.GlobFiles("**/Datadog.AutoInstrumentation.ManagedLoader.csproj").Single());
            projects.Add(ProfilerDirectory.GlobFiles("**/Datadog.AutoInstrumentation.Profiler.Managed.csproj").Single());

            // Build the shared managed loader
            DotNetBuild(s => s
                .SetTargetPlatformAnyCPU()
                .SetConfiguration(BuildConfiguration)
                .CombineWith(projects, (x, project) => x
                    .SetProjectFile(project)));
        });

    Target CompileProfilerNativeSrc => _ => _
        .Unlisted()
        .Description("Compiles the native profiler assets")
        .DependsOn(CompileProfilerNativeSrcWindows);

    Target CompileProfilerNativeSrcWindows => _ => _
        .Unlisted()
        .After(CompileProfilerManagedSrc) // Keeping this because this may depend on embedding managed libs
        .OnlyWhenStatic(() => IsWin)
        .Executes(() =>
        {
            var project = ProfilerDirectory.GlobFiles("**/Datadog.AutoInstrumentation.Profiler.Native.Windows.vcxproj").Single();

            // run: msbuild /property:Configuration=${{matrix.configuration}} /property:Platform=${{matrix.platform}}  dd-continuous-profiler-dotnet\src\ProfilerEngine\Datadog.AutoInstrumentation.Profiler.Native.Windows\Datadog.AutoInstrumentation.Profiler.Native.Windows.WithTests.proj
            // If we're building for x64, build for x86 too
            var platforms =
                Equals(TargetPlatform, MSBuildTargetPlatform.x64)
                    ? new[] { MSBuildTargetPlatform.x64, MSBuildTargetPlatform.x86 }
                    : new[] { MSBuildTargetPlatform.x86 };

            // Can't use dotnet msbuild, as needs to use the VS version of MSBuild
            // Build native profiler assets
            MSBuild(s => s
                .SetTargetPath(project)
                .SetConfiguration(BuildConfiguration)
                .SetProperty("SpectreMitigation", "false") // Enforce the same build in all CI environments
                .SetMSBuildPath()
                .DisableRestore()
                .SetMaxCpuCount(null)
                .CombineWith(platforms, (m, platform) => m
                    .SetTargetPlatform(platform)));
        });
}
