using System.Linq;
using LanguageExt;
using PCE.Chartbuild.Runtime;

namespace PCE.Chartbuild;

// fn (T1, T2, ..T3) -> T4
// fn<T, U>(..T) -> U

public class FunctionType(BaseType returnType, bool isLastParams, params BaseType[] parameterTypes) : BaseType {
    // return type and wether is pure will get calculated later
    // this is a type annotation, the parameter names are not needed
    // public readonly BaseType[] genericTypes;
    public readonly BaseType returnType = returnType;
    // public readonly FunctionArgumentType[] argumentTypes;
    public readonly BaseType[] parameterTypes = parameterTypes;
    public readonly bool isLastParams = isLastParams;

    public override bool IsPureCallable => true;

    //$"fn{(genericTypes is not null && genericTypes.Length > 0 ? $"<{string.Join<BaseType>(", ", genericTypes)}>" : "")}({string.Join<FunctionArgumentType>(", ", argumentTypes)}) -> {returnType}"
    public override string TypeName => $"fn({(isLastParams ? string.Join<BaseType>(", ", parameterTypes[..^2]) + $"..{parameterTypes[^1]}" : string.Join<BaseType>(", ", parameterTypes))}) -> {returnType}";

    public override bool CanCoerceInto(BaseType type) {
        if (type is FunctionType functionType) {
            if (parameterTypes.Length != functionType.parameterTypes.Length || isLastParams != functionType.isLastParams)
            return false;

            return returnType.CanBeAssignedTo(functionType.returnType) && parameterTypes.Zip(functionType.parameterTypes).All((it) => it.Item1.CanBeAssignedTo(it.Item2));
        }

        return false;
    }

    public override Either<ICBValue, ErrorType> Constructor(params ICBValue[] arguments) {
        return ErrorType.NotSupported;
    }
}