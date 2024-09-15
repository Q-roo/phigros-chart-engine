namespace PCE.Chartbuild;

public class UnexpectedTokenException(BaseToken target, string message) : BaseException(target, message)
{
    public UnexpectedTokenException(BaseToken target, TokenType unexpected)
    : this(target, $"expected {target.Type.ToSourceString()} but found {unexpected.ToSourceString()}")
    {

    }
}