using System.Diagnostics;
using LanguageExt;
using PCE.Chartbuild.Runtime;

namespace PCE.Chartbuild;

// for the constructor argument type for types only
public class AnyType : BaseType {
    public override bool IsPureCallable => true;

    public override string TypeName => "Any";

    public override bool CanCoerceInto(BaseType type) {
        return true;
    }

    public override Either<ICBValue, ErrorType> Constructor(params ICBValue[] arguments) {
        throw new UnreachableException(); // this constructor should never be called
    }
}