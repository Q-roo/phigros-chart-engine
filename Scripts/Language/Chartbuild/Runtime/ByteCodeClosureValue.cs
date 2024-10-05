namespace PCE.Chartbuild.Runtime;

public class ByteCodeClosureValue : ObjectValue {
    private readonly UnsafeVM vm;

    public ByteCodeClosureValue(UnsafeVM vm)
    : base(vm) {
        this.vm = vm;
        Type = ValueType.Callable;
    }

    public override CBObject Call(params CBObject[] args) {
        return new(vm.Run(args));
    }
}