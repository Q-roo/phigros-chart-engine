namespace PCE.Chartbuild;

public class ArrayType(BaseType type) : BaseType
{
    public readonly BaseType type = type;

    public override string ToString()
    {
        return $"[{type}]";
    }
}