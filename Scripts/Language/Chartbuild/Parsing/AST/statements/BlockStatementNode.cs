using System.Collections.Generic;
using PCE.Chartbuild.Runtime;

namespace PCE.Chartbuild;

// apparently I can't use params List<T> from my laptop
public class BlockStatementNode(List<StatementNode> body) : StatementNode {
    public readonly List<StatementNode> body = body;
}