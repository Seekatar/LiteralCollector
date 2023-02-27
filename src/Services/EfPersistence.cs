using LiteralCollector.Database;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteralCollector;

internal class EfPersistence : IPersistence
{
    private readonly IConfiguration _configuration;

    public EfPersistence(IConfiguration configuration)
    {
        _configuration = configuration;
        var connectionString = _configuration.GetConnectionString("CodeAnalysis");

    }

    public Dictionary<string, int> Literals { get; private set; } = new Dictionary<string, int>();

    public void Initialize()
    {
        using var db = new LiteralDbContext();

        Literals.Clear();

        Literals = db.Literals.ToDictionary(k => k.Value, v => v.LiteralId);
    }

    public async Task SaveFileScan(int scanId, string filename, Dictionary<string, Tuple<int, int, bool>> locations)
    {
        var sourceFile = new SourceFile() {
            FileName = Path.GetFileName(filename),
            Path = Path.GetDirectoryName(filename),
            ScanId = scanId
        };

        using var db = new LiteralDbContext();
        using var trans = db.Database.BeginTransaction();

        db.SourceFiles.Add(sourceFile);
        await db.SaveChangesAsync();

        foreach (var i in locations)
        {
            if (!Literals.ContainsKey(i.Key))
            {
                try
                {
                    var literal = new Literal()
                    {
                        Value = i.Key,
                    };
                    db.Literals.Add(literal);
                    await db.SaveChangesAsync();
                    Literals.Add(i.Key, literal.LiteralId);
                }
                catch (SqlException e) when (e.Number == 2627)
                {
                    Console.WriteLine($"Duplicate key for {i.Key}"); // get this is odd cases of special characters
                }
            }
            var location = new LiteralLocation()
            {
                LiteralId = Literals[i.Key],
                SourceFileId = sourceFile.SourceFileId,
                LineStart = i.Value.Item1,
                ColumnStart = i.Value.Item2
            };
            location.SourceFileId = sourceFile.SourceFileId;
            db.LiteralLocations.Add(location);
        }
        await db.SaveChangesAsync();
        
        await trans.CommitAsync();
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

    public void Dispose()
    {
        
    }
}
