namespace PCE.Chartbuild;

public class VariableDeclarationStatementNode(bool @readonly, string name, ExpressionNode valueExpression, BaseType type) : StatementNode
{
    public readonly string name = name;

    // const n = ... only creates a read-only variable
    // but it could also be constant if the value can be computed at compile time
    // uninitalized variables cannot be used
    // a value can be assigned to an uninitalized variable
    public readonly bool @readonly = @readonly;
    public readonly bool initalized = valueExpression is not null;

    public readonly ExpressionNode valueExpression = valueExpression;

    // if type is null, try infering it
    public BaseType type = type;
    // infer this as well
    public bool constant {get; private set;}

    // TODO value state: unknown, reducing, reduced
    // TODO type
    // TODO type state: unknown, infered, explicit, infering
}