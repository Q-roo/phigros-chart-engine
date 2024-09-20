using LanguageExt;
using PCE.Chartbuild.Runtime;

namespace PCE.Chartbuild;

// the type object also needs to have a type
public class TypeType : BaseType {
    public override bool IsPureCallable => true;

    public override string TypeName => "Type";

    public override bool CanCoerceInto(BaseType type) {
        return false;
    }

    public override Either<ICBValue, ErrorType> Constructor(params ICBValue[] arguments) {
        return ErrorType.NotSupported;
    }
}