using LanguageExt;
using PCE.Chartbuild.Runtime;

namespace PCE.Chartbuild;

public class F32Type : BaseType {
    public override bool IsPureCallable => true;

    public override string TypeName => "f32";

    public override bool CanCoerceInto(BaseType type) {
        return type switch {
            StringType => true,
            _ => false
        };
    }

    public override Either<ICBValue, ErrorType> Constructor(params ICBValue[] arguments) {
        if (arguments.Length != 1)
            return ErrorType.InvalidArgument;

        return arguments[0] switch {
            I32Value i32 => new F32Value(i32),
            F32Value f32 => f32,
            StringValue str => float.TryParse(str, out float f) ? new F32Value(f) : ErrorType.InvalidArgument,
            _ => ErrorType.InvalidArgument
        };
    }
}