using LanguageExt;
using PCE.Chartbuild.Runtime;

namespace PCE.Chartbuild;

public class StringType : BaseType {
    public override bool IsPureCallable => true;

    public override string TypeName => "str";

    public override bool CanCoerceInto(BaseType type) {
        return false;
    }

    public override Either<ICBValue, ErrorType> Constructor(params ICBValue[] arguments) {
        if (arguments.Length != 1)
            return ErrorType.InvalidArgument;

        return new StringValue(arguments[0].ToString());
    }
}