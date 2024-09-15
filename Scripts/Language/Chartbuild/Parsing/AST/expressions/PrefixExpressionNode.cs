namespace PCE.Chartbuild;

public class PrefixExpressionNode(BaseToken @operator, ExpressionNode expression) : UnaryExpressionNode(expression, @operator, true);