using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteralCollector
{
    internal class Persistence : IPersistence
    {
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

        SqlConnection _conn;

        public Dictionary<string, int> Literals { get; }  = new Dictionary<string, int>(10000);

        public void Dispose()
        {
            _conn?.Dispose();
        }

        public void Initialize()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["CodeAnalysis"];
            _conn = new SqlConnection(connectionString.ConnectionString);
            _conn.Open();

            Literals.Clear();

            using (var cmd = new SqlCommand(LoadLiterals, _conn))
            {
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Literals.Add(reader.GetString(1), reader.GetInt32(0));
                }
            }
        }

        public void Save(string filename, Dictionary<string, Tuple<int, int, bool>> locations)
        {
            using (var cmd = new SqlCommand(InsertFile, _conn))
            {
                cmd.Parameters.AddWithValue("@filename", filename);

                object o = cmd.ExecuteScalar();
                int fileId = (int)o;

                foreach (var i in locations)
                {
                    try
                    {
                        cmd.Parameters.Clear();
                        cmd.CommandText = CheckLiteral;
                        cmd.Parameters.AddWithValue("@literal", i.Key);
                        cmd.Parameters.AddWithValue("@literalId", Literals[i.Key]);
                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException e) when (e.Number == 2627)
                    {
                        Console.WriteLine($"Duplicate key for {i.Key}"); // get this is odd cases of special characters
                    }

                    cmd.Parameters.Clear();
                    cmd.CommandText = InsertLocation;
                    cmd.Parameters.AddWithValue("@literalId", Literals[i.Key]);
                    cmd.Parameters.AddWithValue("@sourceFileId", fileId);
                    cmd.Parameters.AddWithValue("@line", i.Value.Item1);
                    cmd.Parameters.AddWithValue("@char", i.Value.Item2);
                    cmd.Parameters.AddWithValue("@isConstant", i.Value.Item3);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void GetLiteralId(string text)
        {
            lock (Literals)
            {
                var id = Literals.Count + 1;
                if (Literals.ContainsKey(text))
                {
                    id = Literals[text];
                }
                else
                    Literals[text] = id;
            }
        }
    }
}
