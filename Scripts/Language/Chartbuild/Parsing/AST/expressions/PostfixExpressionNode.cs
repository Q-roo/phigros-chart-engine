namespace PCE.Chartbuild;

public class PostfixExpressionNode(ExpressionNode expression, BaseToken @operator) : UnaryExpressionNode(expression, @operator, false);