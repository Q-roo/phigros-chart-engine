using LanguageExt;
using PCE.Chartbuild.Runtime;

namespace PCE.Chartbuild;

public class I32Type : BaseType {
    public override bool IsPureCallable => true;

    public override string TypeName => "i32";

    public override bool CanCoerceInto(BaseType type) {
        return type switch {
            StringType  or
            F32Type or
            BoolType => true,
            _ => false
        };
    }

    public override Either<ICBValue, ErrorType> Constructor(params ICBValue[] arguments) {
        if (arguments.Length != 1)
        return ErrorType.InvalidArgument;

        return arguments[0] switch {
            I32Value i32 => i32,
            F32Value f32 => new I32Value((int)f32.value),
            StringValue str => int.TryParse(str, out int i) ? new I32Value(i) : ErrorType.InvalidArgument,
            BoolValue @bool => new I32Value(@bool ? 1 : 0),
            _ => ErrorType.InvalidArgument
        };
    }
}