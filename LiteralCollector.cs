using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Configuration;



namespace ConsoleApplication1
{
    class LiteralCollector : IDisposable
    {
        SqlConnection _conn;
        Dictionary<string, int> _literals = new Dictionary<string, int>(10000);
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
            Console.WriteLine("Processed {0} files in {1:0.00} minutes", fileCount, sw.Elapsed.TotalMinutes);
            Console.ReadLine();
        }

        private void Process(string path)
        {
            _conn = initDatabase();
            loadLiterals();
            processDirectory(path);
        }

        string[] skips = {"debug","release","database","deploy","packages" };

        /*
        Added parallel without much effect
            Without parallel 1.44-1.52 minutes
            File in parallel 1.25
            File and Direcotry in parallel 1.33
            Set MaxDegreeOfParallelism = 20, 1.27
            Set MaxDegreeOfParallelism = -1, 1.29
        */
        private void processDirectory(string dir)
        {

            if (dir.Length > 1)
            {
                var name = Path.GetFileName(dir).ToLower();
                if (name.StartsWith(".") || skips.Contains(name))
                {
                    Console.WriteLine("Skipping folder " + dir);
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

        private void processFile(string f)
        {
            var context = File.ReadAllText(f);
            var tree = CSharpSyntaxTree.ParseText(context);

            var root = (CompilationUnitSyntax)tree.GetRoot();
            var locations = new Dictionary<string, Tuple<int, int,bool>>(); // literal -> (line,char,isConstant)

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

                        lock( _literals)
                        {
                            var id = _literals.Count + 1;
                            if (_literals.ContainsKey(text))
                            {
                                id = _literals[text];
                            }
                            else
                                _literals[text] = id;
                        }

                        locations[text] = new Tuple<int, int,bool>(loc.StartLinePosition.Line, loc.StartLinePosition.Character,isConstantOrStatic(n));

                        // Console.WriteLine("String {0} at {3} {1}:{2}", n.ToString(), loc.StartLinePosition.Line, loc.StartLinePosition.Character, f);
                    }
                    else
                    {
                        // Console.WriteLine("Something else " + n.ToString());
                    }
            }
            Console.WriteLine(f);

            System.Threading.Interlocked.Increment(ref FileCount);
                
            updateDatabase(f, locations);
        }

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

        const string LoadLiterals = @"SELECT LITERAL_ID, LITERAL FROM LITERAL";

        const string InsertFile = @"
DECLARE @id INT = (SELECT SOURCE_FILE_ID FROM dbo.SOURCE_FILE WHERE FILE_NAME = @filename)
IF @id IS NOT NULL 
BEGIN
    DELETE FROM LITERAL_LOCATION WHERE SOURCE_FILE_ID = @id
END
ELSE
BEGIN
    INSERT INTO [dbo].[SOURCE_FILE]
           ([FILE_NAME])
     VALUES
           (@filename);
    SET @id = @@IDENTITY
END
SELECT @id";

        const string CheckLiteral = @"IF NOT EXISTS (SELECT 1 FROM dbo.LITERAL WHERE LITERAL = @literal)
	INSERT INTO dbo.LITERAL
	        ( LITERAL_ID, LITERAL )
	VALUES  ( @literalId, 
	          @literal  
	          )
";

        const string InsertLocation = @"INSERT INTO dbo.LITERAL_LOCATION
        ( LITERAL_ID ,
          SOURCE_FILE_ID ,
          LINE ,
          CHARACTER,
          IS_CONSTANT
        )
VALUES  ( @literalId,
          @sourceFileId,
          @line,
          @char,
          @isConstant
        )";

        private void updateDatabase(string f, Dictionary<string, Tuple<int, int, bool>> locations)
        {
            var cmd = new SqlCommand(InsertFile, _conn);
            cmd.Parameters.AddWithValue("@filename", f);

            object o = cmd.ExecuteScalar();
            int fileId = (int)o;

            foreach ( var i in locations)
            {
                try
                {
                    cmd.Parameters.Clear();
                    cmd.CommandText = CheckLiteral;
                    cmd.Parameters.AddWithValue("@literal", i.Key);
                    cmd.Parameters.AddWithValue("@literalId", _literals[i.Key]);
                    cmd.ExecuteNonQuery();
                }
                catch ( SqlException e)
                {
                    if (e.Number == 2627)
                        Console.WriteLine("Duplicate key for " + i.Key);
                    else
                        throw;
                }

                cmd.Parameters.Clear();
                cmd.CommandText = InsertLocation;
                cmd.Parameters.AddWithValue("@literalId", _literals[i.Key]);
                cmd.Parameters.AddWithValue("@sourceFileId", fileId);
                cmd.Parameters.AddWithValue("@line", i.Value.Item1);
                cmd.Parameters.AddWithValue("@char", i.Value.Item2);
                cmd.Parameters.AddWithValue("@isConstant", i.Value.Item3);
                cmd.ExecuteNonQuery();
            }
        }

        private void loadLiterals()
        {
            _literals.Clear();

            var cmd = new SqlCommand(LoadLiterals, _conn);
            var reader = cmd.ExecuteReader();
            while ( reader.Read() )
            {
                _literals.Add(reader.GetString(1), reader.GetInt32(0));
            }
        }
        private SqlConnection initDatabase()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["CodeAnalysis"];
            var ret = new SqlConnection(connectionString.ConnectionString);
            ret.Open();
            return ret;
        }

        public void Dispose()
        {
            _conn.Dispose();
        }
    }
}
