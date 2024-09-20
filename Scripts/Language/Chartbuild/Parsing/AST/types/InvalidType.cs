using LanguageExt;
using PCE.Chartbuild.Runtime;

namespace PCE.Chartbuild;

// for the parser for cases like const a: nonexistent_type
public class InvalidType : BaseType {
    public override bool IsPureCallable => true;

    public override string TypeName => "InvalidType";

    public override bool CanCoerceInto(BaseType type) {
        return false;
    }

    public override Either<ICBValue, ErrorType> Constructor(params ICBValue[] arguments) {
        return ErrorType.InvalidType;
    }
}