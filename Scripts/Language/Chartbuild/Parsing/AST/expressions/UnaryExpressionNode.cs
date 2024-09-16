namespace PCE.Chartbuild;

public class UnaryExpressionNode(ExpressionNode expression, BaseToken @operator, bool prefix) : ExpressionNode
{
    public ExpressionNode expression = expression;
    public readonly BaseToken @operator = @operator;
    // true if it's a prefix operation else false
    // ++a <- true, a++ <- false
    public readonly bool prefix = prefix;
}