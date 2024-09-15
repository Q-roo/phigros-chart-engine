namespace PCE.Chartbuild;

public class FunctionArgumentType(BaseType type, bool @params) : BaseType
{
    public readonly BaseType type = type;
    public readonly bool @params = @params;

    public override string ToString()
    {
        return $"{(@params ? ".." : "")}{type}";
    }
}