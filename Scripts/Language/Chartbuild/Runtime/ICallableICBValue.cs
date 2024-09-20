using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public interface ICallableICBValue : ICBValue {
    public bool IsPureCallable {get;}
    public bool IsLastParams {get;}
    public BaseType ReturnType {get;}
    public string[] ParameterNames {get;}
    public BaseType[] ParameterTypes {get;}
    public Either<ICBValue, ErrorType> Call(params ICBValue[] args);
}