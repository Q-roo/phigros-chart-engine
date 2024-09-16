namespace PCE.Chartbuild;

public class FunctionParameter(string name, BaseType type, ExpressionNode defaultValue)
{
    public readonly string name = name;
    public readonly BaseType type = type;
    public readonly ExpressionNode defaultValue = defaultValue;
}