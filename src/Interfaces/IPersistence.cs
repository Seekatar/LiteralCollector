using LiteralCollector.Database;

namespace LiteralCollector;

/// <summary>
/// Get and save off to a database
/// </summary>
internal interface IPersistence : IDisposable
{
    /// <summary>
    /// Initializes this instance.
    /// </summary>
    Task<Project> GetProject(string basePath);

    /// <summary>
    /// Saves the specified filename and data to the database.
    /// </summary>
    /// <param name="filename">The filename.</param>
    /// <param name="locations">The locations, which is literal -> line, char, isConstant.</param>
    Task SaveFileScan(string filename, Dictionary<string, Location> locations);

    /// <summary>
    /// Gets the literals, which is literal->literalId
    /// </summary>
    Dictionary<string, int> Literals { get; }
}
