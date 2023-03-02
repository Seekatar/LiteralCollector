using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Options;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using System;
using LiteralCollector.Database;

namespace LiteralCollector;

/// <summary>
/// main class
/// </summary>
class Collector : IDisposable
{
    private readonly CollectorOptions _options;
    private readonly ILogger<Collector> _logger;
    private readonly IPersistence _conn;
    private int _fileCount;

    public Collector(IOptions<CollectorOptions> options, ILogger<Collector> logger, IPersistence persistence)
    {
        Guard.IsNotNull(options.Value);

        _options = options.Value;
        _logger = logger;
        _conn = persistence;
    }

    /// <summary>
    /// 
    /// </summary>
    public int FileCount => _fileCount;

    /// <summary>
    /// Processes all the cs files in the specified path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="basePath">The path.</param>
    public async Task Process(string path, string basePath)
    {
        var project = await _conn.GetProject(basePath);

        ProcessDirectory(path, project);
    }

    /// <summary>
    /// Recursive method for processing a directory
    /// </summary>
    /// <param name="dir">The dir.</param>
    private void ProcessDirectory(string dir, Project project)
    {
        /*
        Added parallel without much effect
            Without parallel 1.44-1.52 minutes
            File in parallel 1.25
            File and Directory in parallel 1.33
            Set MaxDegreeOfParallelism = 20, 1.27
            Set MaxDegreeOfParallelism = -1, 1.29
        */

        if (dir.Length > 1)
        {
            var name = Path.GetFileName(dir).ToLower();
            if (name.StartsWith(".") || (_options.DirSkips?.Contains(name) ?? false))
            {
                _logger.LogDebug("Skipping folder {dir}", dir);
                return;
            }
        }

#if PARALLEL
        Parallel.ForEach(Directory.EnumerateFiles(dir, "*.cs").Where(o => !o.ToLower().EndsWith("assemblyinfo.cs")),
            new ParallelOptions() { MaxDegreeOfParallelism = -1 }, f => 
#else
        foreach (var f in Directory.EnumerateFiles(dir, "*.cs").Where(o => !o.ToLower().EndsWith("assemblyinfo.cs")))
#endif
        {
            ProcessFile(Path.GetFullPath(f), project);
        }
#if PARALLEL
        );
        Parallel.ForEach(Directory.EnumerateDirectories(dir),
            new ParallelOptions() { MaxDegreeOfParallelism = -1 }, d =>
#else
        foreach (var d in Directory.EnumerateDirectories(dir))
#endif
        {
            var dirName = Path.GetFileName(d);
            if (dirName.Equals("bin", StringComparison.OrdinalIgnoreCase) || dirName.Equals("obj", StringComparison.OrdinalIgnoreCase))
                continue;
            
            ProcessDirectory(d, project);
        }
#if PARALLEL
        );
#endif
    }

    /// <summary>
    /// Processes one file file.
    /// </summary>
    /// <param name="fileName">The fully qualified file name.</param>
    private void ProcessFile(string fileName, Project project)
    {
        if (_options.FileSkips?.Contains(Path.GetFileName(fileName)) ?? false)
        {
            _logger.LogDebug("Skipping file {file}", fileName);
            return;
        }

        var context = File.ReadAllText(fileName);
        var tree = CSharpSyntaxTree.ParseText(context);

        var root = (CompilationUnitSyntax)tree.GetRoot();
        var locations = new Dictionary<string, Location>();

        // get all the string and numeric literals
        var nodes = root.DescendantNodes().Where(o => o.IsKind(SyntaxKind.StringLiteralExpression) 
                                                                                        || o.IsKind(SyntaxKind.NumericLiteralExpression)
                                                                                        || o.IsKind(SyntaxKind.UnaryMinusExpression)
                                                                                        );

        bool haveMinus = false;
        foreach (var nn in nodes)
        {
            if (nn.IsKind(SyntaxKind.UnaryMinusExpression))
            {
                haveMinus = true;
                continue;
            }
            var n = nn as LiteralExpressionSyntax;
            
            if (n!.Token.SyntaxTree is null) continue;
            
            FileLinePositionSpan? loc = n.Token.SyntaxTree?.GetLineSpan(n.Token.Span);

            var text = (haveMinus ? "-" : "")+n.Token.ValueText.Trim();

            // skip simple ones
            if (n.IsKind(SyntaxKind.NumericLiteralExpression) && text != "0" ||
                n.IsKind(SyntaxKind.StringLiteralExpression) && text != "")
            {
                if (text == "1")
                    Console.WriteLine("1");
                if (text.Length > 900)
                    text = string.Concat(text.AsSpan(0, 895), "...");

                foreach (var c in text.ToCharArray())
                {
                    if (Char.IsControl(c) && !Char.IsWhiteSpace(c))
                    {
                        text = text.Replace(c, '~');
                    }
                }

                if (n.IsKind(SyntaxKind.StringLiteralExpression))
                    text = "\"" + text + "\"";

                if (loc is null) continue;
                locations[text] = new Location(loc.Value.StartLinePosition, loc.Value.EndLinePosition, IsConstantOrStatic(n));

            }
            haveMinus = false;
            // else a 0, "" or something we don't care about
        }
        _logger.LogDebug("{file}", fileName);

        Interlocked.Increment(ref _fileCount);

        _conn.SaveFileScan(fileName.Replace(project.BaseFolder,"").Trim(Path.DirectorySeparatorChar), locations);
    }

    /// <summary>
    /// Determines whether the expression is a constant or static
    /// </summary>
    /// <param name="n">The expression.</param>
    /// <returns></returns>
    private static bool IsConstantOrStatic(LiteralExpressionSyntax n)
    {
        if ( n?.Parent?.Parent?.Parent?.Parent is not null && 
             n.Parent.IsKind(SyntaxKind.EqualsValueClause) &&
             n.Parent.Parent.IsKind(SyntaxKind.VariableDeclarator) &&
             n.Parent.Parent.Parent.IsKind(SyntaxKind.VariableDeclaration) &&
             n.Parent.Parent.Parent.Parent.ChildTokens().Any(o => o.IsKind(SyntaxKind.ConstKeyword) || o.IsKind(SyntaxKind.StaticKeyword)))
        {
            return true; // const int i = 0; or static int = 0;
        }
        else
            return false;
    }

    public void Dispose()
    {
        _conn.Dispose();
    }
}
