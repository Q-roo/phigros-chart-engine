using LanguageExt;
using PCE.Chartbuild.Runtime;

namespace PCE.Chartbuild;

public class NullType : BaseType {
    public override bool IsPureCallable => true;

    public override string TypeName => "unset";

    public override bool CanCoerceInto(BaseType type) {
        return false;
    }

    public override Either<ICBValue, ErrorType> Constructor(params ICBValue[] arguments) {
        return new NullValue();
    }
}