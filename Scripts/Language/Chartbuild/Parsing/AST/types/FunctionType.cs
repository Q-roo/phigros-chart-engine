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

    public override string ToString() {
        // return $"fn{(genericTypes is not null && genericTypes.Length > 0 ? $"<{string.Join<BaseType>(", ", genericTypes)}>" : "")}({string.Join<FunctionArgumentType>(", ", argumentTypes)}) -> {returnType}";
        return $"fn({(isLastParams ? string.Join<BaseType>(", ", parameterTypes[..^2]) + $"..{parameterTypes[^1]}" : string.Join<BaseType>(", ", parameterTypes))}) -> {returnType}";
    }
}