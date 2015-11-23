// parameters
var msBuildFileVerbosity = (Verbosity)Enum.Parse(typeof(Verbosity), Environment.GetEnvironmentVariable("MSBUILD_FILE_VERBOSITY") ?? "minimal", true);
var nugetVerbosity = Environment.GetEnvironmentVariable("NUGET_VERBOSITY") ?? "quiet";

// solution specific variables
var nugetCommand = "packages/NuGet.CommandLine.2.8.6/tools/NuGet.exe";
var xunitCommand = "packages/xunit.runners.1.9.2/tools/xunit.console.clr4.exe";
var solution = "src/LibraryHippo.sln";
var output = "artifacts/output";
var tests = "artifacts/tests";
var logs = "artifacts/logs";
var units = new[] { "src/test/LibraryHippo.Test.Unit/bin/Release/LibraryHippo.Test.Unit.dll" };
var component = "src/test/LibraryHippo.Test.Component/bin/Release/LibraryHippo.Test.Component.dll";
var acceptance = "src/test/LibraryHippo.Test.Acceptance/bin/Release/LibraryHippo.Test.Acceptance.dll";

// solution agnostic tasks
var bau = Require<Bau>();

bau
.Task("default").DependsOn(new[] { "unit", "component", "accept", "pack" })

.Task("all").DependsOn("unit", "component", "accept", "pack")

.Task("logs").Do(() => CreateDirectory(logs))

.MSBuild("clean").DependsOn("logs").Do(msb =>
    {
        msb.MSBuildVersion = "net45";
        msb.Solution = solution;
        msb.Targets = new[] { "Clean", };
        msb.Properties = new { Configuration = "Release" };
        msb.MaxCpuCount = -1;
        msb.NodeReuse = false;
        msb.Verbosity = msBuildFileVerbosity;
        msb.NoLogo = true;
        msb.FileLoggers.Add(
            new FileLogger
            {
                FileLoggerParameters = new FileLoggerParameters
                {
                    PerformanceSummary = true,
                    Summary = true,
                    Verbosity = Verbosity.Minimal,
                    LogFile = logs + "/clean.log",
                }
            });
    })

.Task("clobber").DependsOn("clean").Do(() => DeleteDirectory(output))

.Exec("restore").Do(exec => exec
    .Run(nugetCommand).With("restore", "-PackagesDirectory", "packages", "packages.config"))

.MSBuild("build").DependsOn("clean", "restore", "logs").Do(msb =>
    {
        msb.MSBuildVersion = "net45";
        msb.Solution = solution;
        msb.Targets = new[] { "Build", };
        msb.Properties = new { Configuration = "Release" };
        msb.MaxCpuCount = -1;
        msb.NodeReuse = false;
        msb.Verbosity = msBuildFileVerbosity;
        msb.NoLogo = true;
        msb.FileLoggers.Add(
            new FileLogger
            {
                FileLoggerParameters = new FileLoggerParameters
                {
                    PerformanceSummary = true,
                    Summary = true,
                    Verbosity = Verbosity.Minimal,
                    LogFile = logs + "/build.log",
                }
            });
    })

.Task("tests").Do(() => CreateDirectory(tests))

.Xunit("unit").DependsOn("build", "tests").Do(xunit => xunit
    .Use(xunitCommand).Run(units).Html().Xml())

.Xunit("component").DependsOn("build", "tests").Do(xunit => xunit
    .Use(xunitCommand).Run(component).Html().Xml())

.Xunit("accept").DependsOn("build", "tests").Do(xunit => xunit
     .Use(xunitCommand).Run(acceptance).Html().Xml())

.Task("output").Do(() => CreateDirectory(output))
.Run();

void CreateDirectory(string name)
{
    if (!Directory.Exists(name))
    {
        Directory.CreateDirectory(name);
        System.Threading.Thread.Sleep(100); // HACK (adamralph): wait for the directory to be created
    }
}

void DeleteDirectory(string name)
{
    if (Directory.Exists(name))
    {
        Directory.Delete(name, true);
    }
}


