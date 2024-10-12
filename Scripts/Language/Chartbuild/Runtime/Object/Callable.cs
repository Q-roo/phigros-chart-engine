namespace PCE.Chartbuild.Runtime;

public delegate Object CallFunction(params Object[] args);
public delegate void CallFunctionImplicitNullReturn(params Object[] args);
public class Callable(CallFunction value) : Object<CallFunction>(value) {
    public Callable(CallFunctionImplicitNullReturn value)
    : this((args) => {
        value(args);
        return new Unset();
    }) { }

    public override Object Call(params Object[] args) {
        return Value(args);
    }

    public override Object Copy(bool shallow = true, params object[] keys) => this;
}