using System;
using static System.Console; // C# 6 feature
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Configuration;

namespace LiteralCollector
{
    /// <summary>
    /// main class
    /// </summary>
    class LiteralCollector : IDisposable
    {
        string[] _skips = {  }; // dirs
        string[] _fileSkips = {  }; // dirs
        IPersistence _conn;
        int FileCount = 0;

        static void Main(string[] args)
        {
            var sw = new Stopwatch();
            sw.Start();

            var path = ".";
            if (args.Length > 0 )
            {
                if (Directory.Exists(args[0]))
                    path = args[0];
                else
                    throw new ArgumentException("First parameter must be valid path");
            }

            int fileCount = 0;
            using (var lc = new LiteralCollector())
            {

                lc.Process(path);

                lc.Dispose();
                fileCount = lc.FileCount;
            }

            sw.Stop();
            WriteLine($"Processed {fileCount} files in {sw.Elapsed.TotalMinutes:0.00} minutes");

            if (System.Diagnostics.Debugger.IsAttached)
            {
                Write("Press enter to exit ");
                ReadLine();
            }
        }

        /// <summary>
        /// Processes all the cs files in the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        private void Process(string path)
        {
            _conn = new Persistence();
            _conn.Initialize();

            var s = ConfigurationManager.AppSettings["skips"];
            if (s != null )
                _skips = s.Split(",".ToCharArray());

            s = ConfigurationManager.AppSettings["fileskips"];
            if (s != null)
                _fileSkips = s.Split(",".ToCharArray());

            processDirectory(path);
        }

        /// <summary>
        /// Recurisive method for processing a directory
        /// </summary>
        /// <param name="dir">The dir.</param>
        private void processDirectory(string dir)
        {
            /*
            Added parallel without much effect
                Without parallel 1.44-1.52 minutes
                File in parallel 1.25
                File and Direcotry in parallel 1.33
                Set MaxDegreeOfParallelism = 20, 1.27
                Set MaxDegreeOfParallelism = -1, 1.29
            */

            if (dir.Length > 1)
            {
                var name = Path.GetFileName(dir).ToLower();
                if (name.StartsWith(".") || _skips.Contains(name))
                {
                    WriteLine($"Skipping folder {dir}");
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
                processFile(Path.GetFullPath(f));
            }
#if PARALLEL
            );
            Parallel.ForEach(Directory.EnumerateDirectories(dir),
                new ParallelOptions() { MaxDegreeOfParallelism = -1 }, d =>
#else
            foreach (var d in Directory.EnumerateDirectories(dir))
#endif
            {
                processDirectory(d);
            }
#if PARALLEL
            );
#endif
        }

        /// <summary>
        /// Processes one file file.
        /// </summary>
        /// <param name="f">The fully qualified file name.</param>
        private void processFile(string f)
        {
            if (_fileSkips.Contains(Path.GetFileName(f)))
            {
                WriteLine($"Skipping file {f}");
                return;
            }

            var context = File.ReadAllText(f);
            var tree = CSharpSyntaxTree.ParseText(context);

            var root = (CompilationUnitSyntax)tree.GetRoot();
            var locations = new Dictionary<string, Tuple<int, int,bool>>(); // literal -> (line,char,isConstant)

            // get all the string and numeric literals
            var nodes = root.DescendantNodes().OfType<LiteralExpressionSyntax>().Where(o => o.Kind() == SyntaxKind.StringLiteralExpression || o.Kind() == SyntaxKind.NumericLiteralExpression);

            foreach (var n in nodes)
            {
                    var loc = n.Token.SyntaxTree.GetLineSpan(n.Token.Span);

                    var text = n.Token.ValueText.Trim();

                    // skip simple ones
                    if (n.IsKind(SyntaxKind.NumericLiteralExpression) && text != "0" ||
                        n.IsKind(SyntaxKind.StringLiteralExpression) && text != "")
                    {
                        if (text.Length > 900)
                            text = text.Substring(0, 895) + "...";

                        foreach ( var c in text.ToCharArray() )
                        {
                            if (Char.IsControl(c) && !Char.IsWhiteSpace(c) )
                            {
                                text = text.Replace(c, '~');
                            }
                        }

                        if (n.IsKind(SyntaxKind.StringLiteralExpression))
                            text = "\"" + text + "\"";

                        _conn.GetLiteralId(text);

                        // line number is 0-based, but in editor 1 based
                        locations[text] = new Tuple<int, int,bool>(loc.StartLinePosition.Line+1, loc.StartLinePosition.Character,isConstantOrStatic(n));

                    }
                    // else a 0, "" or something we don't care about
            }
            WriteLine(f);

            System.Threading.Interlocked.Increment(ref FileCount);
                
            _conn.Save(f, locations);
        }

        /// <summary>
        /// Determines whether the expression is a constant or static
        /// </summary>
        /// <param name="n">The expression.</param>
        /// <returns></returns>
        private bool isConstantOrStatic(LiteralExpressionSyntax n)
        {
            if (n.Parent.Kind() == SyntaxKind.EqualsValueClause &&
                 n.Parent.Parent.Kind() == SyntaxKind.VariableDeclarator &&
                 n.Parent.Parent.Parent.Kind() == SyntaxKind.VariableDeclaration &&
                 n.Parent.Parent.Parent.Parent.ChildTokens().Any(o => o.Kind() == SyntaxKind.ConstKeyword || o.Kind() == SyntaxKind.StaticKeyword))
            {
                return true; //  const int i = 0; or static int = 0;
            }
            else
                return false;
        }

        public void Dispose()
        {
            _conn.Dispose();
        }
    }
}
