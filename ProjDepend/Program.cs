using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;

internal class Program
{
    private static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: projdepend <path-to-sln>");
            return;
        }

        var solutionPath = args[0];

        if (!File.Exists(solutionPath))
        {
            Console.WriteLine($"Solution file not found: {solutionPath}");
            return;
        }

        MSBuildLocator.RegisterDefaults();

        var projectDependencies = ParseSolution(solutionPath);
        var dotGraph = GenerateDotGraph(projectDependencies);

        var outputPath = Path.ChangeExtension(solutionPath, ".dot");
        File.WriteAllText(outputPath, dotGraph);

        Console.WriteLine($"Dependency graph written to {outputPath}");
    }

    private static Dictionary<string, List<string>> ParseSolution(string solutionPath)
    {
        var dependencies = new Dictionary<string, List<string>>();

        var solutionDirectory = Path.GetDirectoryName(solutionPath);

        foreach (var projectFile in Directory.EnumerateFiles(solutionDirectory, "*.csproj", SearchOption.AllDirectories))
        {
            var project = new Project(projectFile);
            var projectName = Path.GetFileNameWithoutExtension(projectFile);

            if (!dependencies.ContainsKey(projectName))
                dependencies[projectName] = new List<string>();

            foreach (var reference in project.GetItems("ProjectReference"))
            {
                var referencedProjectPath = reference.EvaluatedInclude;
                var referencedProjectName = Path.GetFileNameWithoutExtension(referencedProjectPath);

                dependencies[projectName].Add(referencedProjectName);
            }
        }

        return dependencies;
    }

    private static string GenerateDotGraph(Dictionary<string, List<string>> dependencies)
    {
        var writer = new StringWriter();

        writer.WriteLine("digraph G {");
        writer.WriteLine(@"  rankdir=LR;
  fontname=""Helvetica,Arial,sans-serif""
  node [fontname=""Helvetica,Arial,sans-serif""]
  edge [fontname=""Helvetica,Arial,sans-serif""]
  node [fontsize=10, shape=box, height=0.25]
  edge [fontsize=10]");

        foreach (var project in dependencies)
        {
            foreach (var dependency in project.Value)
            {
                writer.WriteLine($"  \"{project.Key}\" -> \"{dependency}\";");
            }
        }

        writer.WriteLine("}");
        return writer.ToString();
    }

}
