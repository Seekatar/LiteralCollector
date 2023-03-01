using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LiteralCollector.Database;

internal class LiteralDbContext : DbContext
{
    public DbSet<Scan> Scans{ get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Literal> Literals { get; set; }
    public DbSet<SourceFile> SourceFiles { get; set; }
    public DbSet<LiteralLocation> LiteralLocations{ get; set; }

    public string DbPath { get; }

    public LiteralDbContext(string? _connectionString)
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = Path.Join(path, "literals.db");
    }

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Literal>()
            .HasAlternateKey(l => l.Value)
            .HasName("AK_Literal_Value");

        modelBuilder.Entity<SourceFile>()
                    .HasMany(p => p.Literals)
                    .WithMany(p => p.SourceFiles)
                    .UsingEntity<LiteralLocation>(
                        j => j
                            .HasOne(pt => pt.Literal)
                            .WithMany(t => t.LiteralLocations)
                            .HasForeignKey(pt => pt.LiteralId),
                        j => j
                            .HasOne(pt => pt.SourceFile)
                            .WithMany(t => t.LiteralLocations)
                            .HasForeignKey(pt => pt.SourceFileId),
                        j =>
                        {
                            j.Property(pt => pt.LineStart).HasDefaultValueSql("0");
                            j.Property(pt => pt.LineEnd).HasDefaultValueSql("0");
                            j.Property(pt => pt.ColumnStart).HasDefaultValueSql("0");
                            j.Property(pt => pt.ColumnEnd).HasDefaultValueSql("0");
                            j.HasKey(t => new { t.SourceFileId, t.LiteralId, t.LineStart, t.ColumnStart });
                        });
    }
}


#nullable disable
public class Project
{
    public int ProjectId { get; set; }
    [Required]
    [MaxLength(255)]
    public string Url { get; set; }
    [Required]
    [MaxLength(255)]
    public string BaseFolder { get; set; }
    [Required]
    [MaxLength(255)]
    public string HostName { get; set; }
    
    public List<Scan> Scans { get; } = new();
}

public class Scan
{
    public int ScanId { get; set; }
    [Required]
    public DateTimeOffset StartTime { get; set; }
    [Required]
    [MaxLength(255)]
    public string Url { get; set; }
    [Required]
    public int LiteralCount { get; set; }
    [Required]
    public int PreviousLiteralCount { get; set; }
    [Required]
    public int DurationSecs { get; set; }

    [Required]
    public int ProjectId { get; set; }
    public Project Project { get; set; }
    
    public List<SourceFile> SourceFiles { get; } = new();
}

public class SourceFile
{ 
    public int SourceFileId { get; set; }
    [Required]
    [MaxLength(255)]
    public string Path { get; set; }
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; }

    public ICollection<Literal> Literals { get; set; }
    public List<LiteralLocation> LiteralLocations { get; set; }
    
    
    [Required]
    public int ScanId { get; set; }
    public Scan Scan { get; set; }
}

public class Literal
{
    public int LiteralId { get; set; }
    [Required]
    [MaxLength(255)]
    public string Value { get; set; }
    public ICollection<SourceFile> SourceFiles { get; set; }
    public List<LiteralLocation> LiteralLocations { get; set; }

}

[PrimaryKey(nameof(LiteralId), nameof(SourceFileId), nameof(LineStart), nameof(ColumnStart))]
public class LiteralLocation
{
    [Required]
    public int LiteralId { get; set; }
    public Literal Literal { get; set; }
    [Required]
    public int SourceFileId { get; set; }
    public SourceFile SourceFile { get; set; }
    [Required]
    public int LineStart { get; set; }
    [Required]
    public int LineEnd{ get; set; }
    [Required]
    public int ColumnStart { get; set; }
    [Required]
    public int ColumnEnd { get; set; }
}

