using CommunityToolkit.Diagnostics;
using LiteralCollector.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

namespace LiteralCollector;

internal class EfPersistence : IPersistence
{
    private readonly IConfiguration _configuration;
    private readonly string? _connectionString;
    private Scan? _scan;

    public EfPersistence(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = _configuration.GetConnectionString("CodeAnalysis");
    }

    public Dictionary<string, int> Literals { get; private set; } = new Dictionary<string, int>();

    public async Task Initialize(string basePath)
    {
        using var db = new LiteralDbContext(_connectionString);

        var project = await db.Projects.Include(o => o.Scans).FirstOrDefaultAsync(o => o.BaseFolder == basePath);
        project ??= new Project { BaseFolder = basePath };
        _scan = new Scan {
            StartTime = DateTimeOffset.Now,
        };
        project.Scans.Add(_scan);
        
        await db.SaveChangesAsync();
        
        Literals.Clear();

        Literals = db.Literals.ToDictionary(k => k.Value, v => v.LiteralId);
    }

    public async Task SaveFileScan(int scanId, string filename, Dictionary<string, Location> locations)
    {
        Guard.IsNotNull(_scan);
        
        var sourceFile = new SourceFile() {
            FileName = Path.GetFileName(filename),
            Path = Path.GetDirectoryName(filename),
            ScanId = _scan.ScanId
        };

        using var db = new LiteralDbContext(_connectionString);
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
                LineStart = i.Value.Start.Line,
                ColumnStart = i.Value.Start.Character,
                LineEnd = i.Value.End.Line,
                ColumnEnd = i.Value.End.Character,
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
