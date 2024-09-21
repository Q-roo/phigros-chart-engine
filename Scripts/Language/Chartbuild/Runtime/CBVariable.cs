using System.Diagnostics;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;

namespace PCE.Chartbuild.Runtime;

public class CBVariable {
    private ICBValue value;
    public readonly bool @readonly;
    public readonly bool constant; // as in compile time constant
    public bool initalized { get; private set; }

    private CBVariable(ICBValue value, bool @readonly, bool initalized, bool constant) {
        this.value = value;
        this.@readonly = @readonly;
        this.initalized = initalized;
        this.constant = constant;
    }

    public CBVariable(bool @readonly)
    : this(null, @readonly, false, true) { }
    public CBVariable(bool @readonly, bool constant)
    : this(null, @readonly, false, constant) {}
    public CBVariable(bool @readonly, bool initalized, bool constant)
    : this(null, @readonly, initalized, constant) {}
    public CBVariable(ICBValue value, bool @readonly, bool constant)
    : this(value, @readonly, value is not null, constant) {}
    public CBVariable(ICBValue value, bool @readonly)
    : this(value, @readonly, value is not null, true) { }

    public ICBValue GetValueUnsafe() => value;
    public void SetValueUnsafe(ICBValue value) => this.value = value.IsReference ? value : value.Clone().Swap().ValueUnsafe();
    public Either<ICBValue, ErrorType> GetValue() => initalized ? Either<ICBValue, ErrorType>.Left(value) : ErrorType.UninitalizedValue;
    // for the wm when it is known that it is initalized for sure
    public ErrorType SetValue(ICBValue value) {
        if (initalized && @readonly)
            return ErrorType.SetConstant;

        if (value is null || value is NullValue)
            return ErrorType.SetNull;

        if (value.IsReference) {
            this.value = value;
        } else {
            switch (value.Clone().Case) {
                case ICBValue ok:
                    this.value = ok;
                    break;
                case ErrorType err:
                    return err;
                default:
                    throw new UnreachableException();
            }
        }

        if (!initalized)
            initalized = true;
        return ErrorType.NoError;
    }
}