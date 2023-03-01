using Microsoft.CodeAnalysis.Text;

namespace LiteralCollector;
internal record Location(LinePosition Start, LinePosition End, bool IsConstant);

