namespace PCE.Chartbuild;

public class FunctionParameter(string name, FunctionArgumentType type, ExpressionNode defaultValue)
{
    public readonly string name = name;
    public readonly FunctionArgumentType type = type;
    public readonly ExpressionNode defaultValue = defaultValue;
}