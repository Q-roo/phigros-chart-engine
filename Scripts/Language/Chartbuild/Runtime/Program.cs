using System.Collections.Generic;

namespace PCE.Chartbuild.Runtime;

public class Program {
    public readonly Scope scope;
    public readonly List<StatementNode> body;

    public Program(List<StatementNode> body) {
        this.body = body;
        scope = new();
        
    }
}