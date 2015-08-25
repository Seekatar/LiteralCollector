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

namespace ConsoleApplication1
{
    class LiteralCollector : IDisposable
    {
        SqlConnection _conn;
        Dictionary<string, int> _literals = new Dictionary<string, int>(10000);
        const int x = 1;
        static int y = 2;

        static void Main(string[] args)
        {
            var path = ".";
            if (args.Length > 0 )
            {
                if (Directory.Exists(args[0]))
                    path = args[0];
                else
                    throw new ArgumentException("First parameter must be valid path");
            }

            using (var lc = new LiteralCollector())
            {

                lc.Process();

                lc.Dispose();
            }

        }

        private void Process()
        {
            _conn = initDatabase();
            loadLiterals();
            processDirectory(".");
        }

        private void processDirectory(string dir)
        {
            foreach (var d in Directory.EnumerateDirectories(dir).Where( o => !(o.Contains(@"\Debug\") || o.Contains(@"\Release\"))))
            {
                foreach (var f in Directory.EnumerateFiles(d, "*.cs").Where(o => !o.ToLower().EndsWith("assemblyinfo.cs")))
                {
                    processFile(Path.GetFullPath(f));
                }
                foreach (var dd in Directory.EnumerateDirectories(d))
                {
                    processDirectory(dd);
                }
            }
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
                        if (text.Length > 1000)
                            text = text.Substring(0, 905) + "...";

                        if (n.IsKind(SyntaxKind.StringLiteralExpression))
                            text = "\"" + text + "\"";

                        var id = _literals.Count + 1;
                        if (_literals.ContainsKey(text))
                        {
                            id = _literals[text];
                        }
                        else
                            _literals[text] = id;

                        locations[text] = new Tuple<int, int,bool>(loc.StartLinePosition.Line, loc.StartLinePosition.Character,isConstantOrStatic(n));

                        // Console.WriteLine("String {0} at {3} {1}:{2}", n.ToString(), loc.StartLinePosition.Line, loc.StartLinePosition.Character, f);
                    }
                    else
                    {
                        // Console.WriteLine("Something else " + n.ToString());
                    }
            }
            Console.WriteLine(f);

            updateDatabase(f, locations);
        }

        private bool isConstantOrStatic(LiteralExpressionSyntax n)
        {
            if (n.Parent.Kind() == SyntaxKind.EqualsValueClause &&
                 n.Parent.Parent.Kind() == SyntaxKind.VariableDeclarator &&
                 n.Parent.Parent.Parent.Kind() == SyntaxKind.VariableDeclaration &&
                 n.Parent.Parent.Parent.ChildNodes().Any(o => o.Kind() == SyntaxKind.ConstKeyword || o.Kind() == SyntaxKind.StaticKeyword))
            {
                return true; //  const int i = 0; or static int = 0;
            }
            else
                return false;
        }

        const string LoadLiterals = @"SELECT LITERAL_ID, LITERAL FROM LITERAL";

        const string InsertFile = @"INSERT INTO [dbo].[SOURCE_FILE]
           ([FILE_NAME])
     VALUES
           (@filename);
        SELECT @@IDENTITY";

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
            int fileId = (int)(decimal)o;

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
            while ( reader.NextResult() )
            {
                _literals.Add((string)reader[0], (int)reader[1]);
            }
        }
        private SqlConnection initDatabase()
        {
            var ret = new SqlConnection(@"Server=localhost;Initial Catalog=CODE_ANALYSIS;Integrated Security=True;MultipleActiveResultSets=True;Max Pool Size=100;Min Pool Size=0;Pooling=True");
            ret.Open();
            return ret;
        }

        public void Dispose()
        {
            _conn.Dispose();
        }
    }
}
