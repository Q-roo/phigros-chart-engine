using System.Numerics;
using Godot;

namespace PCE.Editor;

public partial class NumberLiteralGraphNode<T> : ValueContainerGraphNode<double> where T : INumber<T> {
    protected readonly SpinBox literal = new();
    public override double Value { get => literal.Value; protected set => literal.Value = value; }
    public T NumericValue { get => T.CreateChecked(Value); set => Value = double.CreateChecked(value); }

    public NumberLiteralGraphNode() {
        literal.AllowLesser = true;
        literal.AllowGreater = true;
        AddChild(literal);
    }
}