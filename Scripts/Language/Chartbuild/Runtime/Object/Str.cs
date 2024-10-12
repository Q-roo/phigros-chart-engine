using System.Collections.Generic;
using System.Diagnostics;

namespace PCE.Chartbuild.Runtime;

public class Str : Object<string> {
    private readonly ReadOnlyProperty _length;
    public Str(string value)
    : base(value) {
        _length = new(this, "length", (_, key) => {
            Debug.Assert(key.Equals("length"));
            return Value.Length;
        });
    }
    public override Property GetProperty(object key) => key switch {
        int idx => new ReadOnlyProperty(this, idx, (_, idx) => Value[(int)idx]),
        "length" => _length,
        _ => base.GetProperty(key),
    };

    public override Object BinaryOperation(OperatorType @operator, Object rhs) => @operator switch {
        OperatorType.Plus => Value + rhs.ToString(),
        _ => base.BinaryOperation(@operator, rhs)
    };

    public override IEnumerator<Object> GetEnumerator() {
        foreach (char c in Value)
            yield return c;
    }

    public override bool ToBool() => !string.IsNullOrEmpty(Value);

    public override Object Copy(bool shallow = true, params object[] keys) => shallow ? Value : new string(Value);
}