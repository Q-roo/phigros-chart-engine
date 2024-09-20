using LanguageExt;
using PCE.Chartbuild.Runtime;

namespace PCE.Chartbuild;

public class ArrayType(BaseType type) : IEnumerableType(type) {
    public override bool IsPureCallable => true;

    public override string TypeName => $"[{type}]";

    // TODO: coercion and constructor
    public override bool CanCoerceInto(BaseType type) {
        return base.CanCoerceInto(type); // anything that's an IEnumerable can be made into an array
    }

    public override Either<ICBValue, ErrorType> Constructor(params ICBValue[] arguments) {
        ArrayValue array = new();
        foreach (ICBValue argument in arguments) {
            if (!argument.Type.CanBeAssignedTo(type))
                return ErrorType.InvalidType;

            array.AddMember(argument);
        }

        return array;
    }
}