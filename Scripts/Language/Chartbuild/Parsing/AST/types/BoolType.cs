using LanguageExt;
using PCE.Chartbuild.Runtime;

namespace PCE.Chartbuild;

public class BoolType : BaseType {
    public override bool IsPureCallable => true;

    public override string TypeName => "bool";

    public override bool CanCoerceInto(BaseType type) {
        return type switch {
            StringType or
            I32Type => true,
            _ => false
        };
    }

    public override Either<ICBValue, ErrorType> Constructor(params ICBValue[] arguments) {
        if (arguments.Length != 1)
            return ErrorType.InvalidArgument;

        return arguments[0] switch {
            I32Value i32 => new BoolValue(i32 == 1),
            StringValue str => bool.TryParse(str, out bool b) ? new BoolValue(b) : ErrorType.InvalidArgument,
            _ => ErrorType.InvalidArgument
        };
    }
}