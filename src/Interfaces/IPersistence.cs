using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteralCollector
{
    /// <summary>
    /// Get and save off to a database
    /// </summary>
    internal interface IPersistence : IDisposable
    {
        /// <summary>
        /// Initializes this instance.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Saves the specified filename and data to the database.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="locations">The locations, which is literal -> line, char, isConstant.</param>
        void Save(string filename, Dictionary<string, Tuple<int, int, bool>> locations);

        /// <summary>
        /// Gets the literals, which is literal->literalId
        /// </summary>
        Dictionary<string, int> Literals { get; }

        /// <summary>
        /// Gets the literal identifier, add Literals if not there
        /// </summary>
        /// <param name="text">The text.</param>
        void GetLiteralId(string text);
    }
}
