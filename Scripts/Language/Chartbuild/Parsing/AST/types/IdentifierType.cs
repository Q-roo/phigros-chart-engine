namespace PCE.Chartbuild;

public class IdentifierType(string name) : BaseType
{
    public readonly string name = name;

    public override string ToString()
    {
        return name;
    }
}