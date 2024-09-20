using System.Diagnostics;
using LanguageExt;
using PCE.Chartbuild.Runtime;

namespace PCE.Chartbuild;

public abstract class BaseType : ICallableICBValue {
    public virtual BaseType Parent => null;
    public bool IsReference => true;
    public abstract bool IsPureCallable { get; }
    public abstract string TypeName { get; }

    public BaseType Type => new TypeType();

    // constructors receive only 1 parameter for now
    // TODO: probably overloading
    public bool IsLastParams => false;

    // this will become the child class instance
    public BaseType ReturnType => this;

    public string[] ParameterNames => ["input"];

    public BaseType[] ParameterTypes => [new AnyType()];

    public sealed override string ToString() => TypeName;

    public static bool operator ==(BaseType left, BaseType right) => left.Equals(right);
    public static bool operator !=(BaseType left, BaseType right) => !left.Equals(right);
    public bool Equals(BaseType right) => TypeName == right.TypeName;
    public override bool Equals(object obj) => obj is BaseType rigth && rigth == this;

    public bool CanBeAssignedTo(BaseType other) => this == other
    || IsChildOf(other)
    || CanCoerceInto(other);

    public override int GetHashCode() => ToString().GetHashCode();

    public virtual bool IsChildOf(BaseType parent) {
        BaseType p = Parent;
        while (p is not null) {
            if (p == parent)
                return true;

            p = p.Parent;
        }

        return false;
    }

    public Either<ICBValue, ErrorType> Call(params ICBValue[] args) => Constructor(args);

    public abstract bool CanCoerceInto(BaseType type);
    public abstract Either<ICBValue, ErrorType> Constructor(params ICBValue[] arguments);

    public object GetValue() {
        return this;
    }

    public ErrorType SetValue(object value) {
        return ErrorType.SetConstant;
    }

    public Either<ICBValue, ErrorType> ExecuteBinaryOperator(TokenType @operator, ICBValue rhs) {
        return ErrorType.NotSupported;
    }

    public ICBValue ExecuteBinaryOperatorUnsafe(TokenType @operator, ICBValue rhs) {
        throw new UnreachableException();
    }
}