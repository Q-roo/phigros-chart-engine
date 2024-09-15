namespace PCE.Chartbuild;

public abstract class LoopStatementNode : StatementNode
{
    public BlockStatementNode body;

    public LoopStatementNode(StatementNode body)
    {
        this.body =  body is BlockStatementNode block ? block : new([body]);

    }
}