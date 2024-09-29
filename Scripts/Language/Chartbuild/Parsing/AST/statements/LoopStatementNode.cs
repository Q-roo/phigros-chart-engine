namespace PCE.Chartbuild;

public abstract class LoopStatementNode(StatementNode body) : StatementNode
{
    public BlockStatementNode body =  body is BlockStatementNode block ? block : new([body]);
}