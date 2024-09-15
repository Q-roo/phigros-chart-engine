using System.Collections.Generic;
using PCE.Chartbuild.Runtime;

namespace PCE.Chartbuild;

public class BlockStatementNode(params List<StatementNode> body) : StatementNode {
    public readonly List<StatementNode> body = body;
    public readonly Scope scope = new();

    public void RemoveChildStatementNode(StatementNode child) {
        body.Remove(child);
    }
}