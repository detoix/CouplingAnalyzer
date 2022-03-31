using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Codartis.NsDepCop.ParserAdapter.Roslyn;
using Codartis.NsDepCop.Analysis;

namespace CouplingAnalyzer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Attempt to set the version of MSBuild.
            var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            var instance = (visualStudioInstances.Length == 1 || args.Length > 1)
                // If there is only one instance of MSBuild on this machine or test flag is used, set that as the one to use.
                ? visualStudioInstances[0]
                // Handle selecting the version of MSBuild you want to use.
                : SelectVisualStudioInstance(visualStudioInstances);

            Console.WriteLine($"Using MSBuild at '{instance.MSBuildPath}' to load projects.");

            // NOTE: Be sure to register an instance with the MSBuildLocator 
            //       before calling MSBuildWorkspace.Create()
            //       otherwise, MSBuildWorkspace won't MEF compose.
            MSBuildLocator.RegisterInstance(instance);
            
            using (var workspace = MSBuildWorkspace.Create())
            {
                // Print message for WorkspaceFailed event to help diagnosing project load failures.
                workspace.WorkspaceFailed += (o, e) => Console.WriteLine(e.Diagnostic.Message);

                var solutionPath = args[0];
                var rootFinder = new GitRootFinder(solutionPath);
                var progressReporter = new ConsoleProgressReporter();
                var nodeAnalyzer = new CouplingToClassesFinder(rootFinder.RootDirectory());
                var dependencies = new HashSet<TypeDependency>();
                Console.WriteLine($"Loading solution '{solutionPath}'");

                // Attach progress reporter so we print projects as they are loaded.
                var solution = await workspace.OpenSolutionAsync(solutionPath, progressReporter);
                Console.WriteLine($"Finished loading solution '{solutionPath}'");

                var projectsCount = solution.Projects.Count();
                foreach (var project in solution.Projects)
                {
                    System.Console.WriteLine($"{projectsCount--} projects to go, processing {project.Name}");

                    foreach (var document in project.Documents)
                    {
                        var semanticModel = await document.GetSemanticModelAsync();
                        var tree = await document.GetSyntaxTreeAsync();

                        foreach (var syntaxNode in tree.GetRoot().DescendantNodes().OfType<SyntaxNode>())
                        {
                            try
                            {
                                foreach (var dependency in nodeAnalyzer.GetDependenciesOtherThanSystem(syntaxNode, semanticModel, document))
                                {
                                    dependencies.Add(dependency);
                                }
                            }
                            catch (Exception ex)
                            {
                                dependencies.Add(new TypeDependency(
                                    string.Empty, ex.Message, string.Empty, ex.StackTrace, new SourceSegment(0, 0, 0, 0, "|", "|")));
                            }
                        }
                    }
                }

                var reportPath = Path.Combine(
                    Path.GetDirectoryName(solutionPath),
                    $"{Path.GetFileNameWithoutExtension(solutionPath)}.tsv");
                var content = Enumerable.Empty<string>()
                    .Append("FromProject\tFromFile\tFromType\tToProject\tToFile\tToType")
                    .Concat(dependencies.Select(e => $"{e.SourceSegment.Text.Split('|')[0]}\t{e.SourceSegment.Text.Split('|')[1]}\t{e.FromNamespaceName}.{e.FromTypeName}\t{e.SourceSegment.Path.Split('|')[0]}\t{e.SourceSegment.Path.Split('|')[1]}\t{e.ToNamespaceName}.{e.ToTypeName}"));

                File.WriteAllLines(reportPath, content);
            }
        }

        private static VisualStudioInstance SelectVisualStudioInstance(VisualStudioInstance[] visualStudioInstances)
        {
            Console.WriteLine("Multiple installs of MSBuild detected please select one:");
            for (int i = 0; i < visualStudioInstances.Length; i++)
            {
                Console.WriteLine($"Instance {i + 1}");
                Console.WriteLine($"    Name: {visualStudioInstances[i].Name}");
                Console.WriteLine($"    Version: {visualStudioInstances[i].Version}");
                Console.WriteLine($"    MSBuild Path: {visualStudioInstances[i].MSBuildPath}");
            }

            while (true)
            {
                var userResponse = Console.ReadLine();
                if (int.TryParse(userResponse, out int instanceNumber) &&
                    instanceNumber > 0 &&
                    instanceNumber <= visualStudioInstances.Length)
                {
                    return visualStudioInstances[instanceNumber - 1];
                }
                Console.WriteLine("Input not accepted, try again.");
            }
        }

        private class ConsoleProgressReporter : IProgress<ProjectLoadProgress>
        {
            public void Report(ProjectLoadProgress loadProgress)
            {
                var projectDisplay = Path.GetFileName(loadProgress.FilePath);
                if (loadProgress.TargetFramework != null)
                {
                    projectDisplay += $" ({loadProgress.TargetFramework})";
                }

                Console.WriteLine($"{loadProgress.Operation,-15} {loadProgress.ElapsedTime,-15:m\\:ss\\.fffffff} {projectDisplay}");
            }
        }
    }

    class GitRootFinder
    {
        private readonly string _solutionPath;

        internal GitRootFinder(string solutionPath)
        {
            this._solutionPath = solutionPath;
        }

        internal string RootDirectory()
        {
            var directory = new DirectoryInfo(Path.GetDirectoryName(this._solutionPath));
            while(directory.GetDirectories(".git").Length == 0)
            {
                directory = directory.Parent;

                if(directory == null)
                {
                    throw new DirectoryNotFoundException("We went all the way up to the system root directory and didn't find any \".git\" directory!");
                }
            }
            
            return directory.FullName;
        }
    }

    class CouplingToClassesFinder : SyntaxNodeAnalyzer
    {
        private readonly string _rootDirectory;
        private readonly HashSet<MethodKind> _allowedMethodKinds;
        private readonly IDictionary<string, ITypeSymbol> _assembliesByPath;

        internal CouplingToClassesFinder(string rootDirectory)
        {
            this._rootDirectory = rootDirectory;
            this._allowedMethodKinds = new HashSet<MethodKind>() { MethodKind.PropertyGet, MethodKind.PropertySet, MethodKind.Constructor };
            this._assembliesByPath = new Dictionary<string, ITypeSymbol>();
        }

        internal IEnumerable<TypeDependency> GetDependenciesOtherThanSystem(SyntaxNode node, SemanticModel semanticModel, Document document)
        {
            var result = this.GetTypeDependencies(node, semanticModel)
                .Where(e => !e.ToNamespaceName.StartsWith(nameof(System)))
                .Where(e => e.FromNamespaceName != e.ToNamespaceName || e.FromTypeName != e.ToTypeName)
                .Select(e =>
                {
                    var itemLocation = this.LocationOf(e);
                    var response = new TypeDependency(
                        e.FromNamespaceName,
                        e.FromTypeName,
                        e.ToNamespaceName,
                        e.ToTypeName,
                        new SourceSegment(0, 0, 0, 0,
                            string.Join("|", document.Project.Name, document.FilePath.Replace(_rootDirectory + Path.DirectorySeparatorChar, string.Empty)), 
                            string.Join("|", itemLocation.Project, itemLocation.FilePath.Replace(_rootDirectory + Path.DirectorySeparatorChar, string.Empty))));

                    return response;
                });

            return result;
        }

        private (string Project, string FilePath) LocationOf(TypeDependency typeDependency) => this._assembliesByPath.TryGetValue(typeDependency.SourceSegment.ToString(), out var result)
            ? (result.ContainingAssembly.Name, result.Locations[0].GetLineSpan().Path) : ("Unknown", "Unknown");

        protected override IEnumerable<ITypeSymbol> GetConstituentTypes(ITypeSymbol typeSymbol, SyntaxNode syntaxNode)
        {
            foreach (var item in base.GetConstituentTypes(typeSymbol, syntaxNode).Where(e => e.TypeKind == TypeKind.Class))
            {
                var members = item.GetMembers().ToList();
                var baseType = item.BaseType;

                while (baseType != null && baseType.Name != nameof(Object))
                {
                    members.AddRange(baseType.GetMembers());
                    baseType = baseType.BaseType;
                }

                var onlyFieldsPropertiesAndConstructors = members.All(e => e.Kind == SymbolKind.Field
                    || e.Kind == SymbolKind.Property
                    || (e is IMethodSymbol method && this._allowedMethodKinds.Contains(method.MethodKind)));

                if (!onlyFieldsPropertiesAndConstructors && !item.ContainingNamespace.ToDisplayString().StartsWith(nameof(System)))
                {
                    var lineSpan = syntaxNode.GetLocation().GetLineSpan();
                    var key = new SourceSegment
                    (
                        lineSpan.StartLinePosition.Line + 1,
                        lineSpan.StartLinePosition.Character + 1,
                        lineSpan.EndLinePosition.Line + 1,
                        lineSpan.EndLinePosition.Character + 1,
                        syntaxNode.ToString(),
                        lineSpan.Path
                    ).ToString();

                    if (!this._assembliesByPath.ContainsKey(key))
                    {
                        this._assembliesByPath.Add(key, item);
                    }

                    yield return item;
                }
            }
        }
    }
}