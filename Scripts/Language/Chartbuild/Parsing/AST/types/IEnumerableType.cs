using LanguageExt;
using PCE.Chartbuild.Runtime;

namespace PCE.Chartbuild;

// TODO: child of generic type
public class IEnumerableType(BaseType type) : BaseType {
    public override bool IsPureCallable => true;
    public BaseType type = type;

    public override string TypeName => $"IEnumerable<{Type}>";

    public override bool CanCoerceInto(BaseType type) {
        return type.IsChildOf(this);
    }

    public override Either<ICBValue, ErrorType> Constructor(params ICBValue[] arguments) {
        if (arguments.Length == 0) return ErrorType.InvalidArgument;
        else if (arguments.Length == 1 && arguments[0].Type.IsChildOf(this))
            return Either<ICBValue, ErrorType>.Left(arguments[0]);
        else {
            return new ArrayType(arguments[0].Type).Constructor(arguments);
        }
    }
}