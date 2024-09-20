using LanguageExt;
using PCE.Chartbuild.Runtime;

namespace PCE.Chartbuild;

public class ArrayType(BaseType type) : IEnumerableType(type)
{
    public override bool IsPureCallable => true;

    public override string TypeName => $"[{type}]";

    // TODO: coercion and constructor
    public override bool CanCoerceInto(BaseType type) {
        return type.CanBeAssignedTo(new IEnumerableType(type));
    }

    public override Either<ICBValue, ErrorType> Constructor(params ICBValue[] arguments) {
        throw new System.NotImplementedException();
    }
}