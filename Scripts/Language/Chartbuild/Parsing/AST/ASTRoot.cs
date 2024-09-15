using System.Collections.Generic;

namespace PCE.Chartbuild;

public class ASTRoot(List<StatementNode> body, List<Error> errors) : BlockStatementNode(body)
{
    // not an array because the analyser can still discover new errors
    public readonly List<Error> errors = errors;
    // TODO: warning list
}