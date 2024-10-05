using System;
using System.Collections.Generic;

namespace PCE.Chartbuild.Runtime;

public class Str(string value) : Object {
    public string value = value;
    public override object Value => value;

    public override Object this[object key] {
        get => key switch {
            int idx => new Str(new(value[idx], 1)),
            "length" => throw new NotImplementedException(),
            _ => throw KeyNotFound(key)
        };
        set => throw ReadOnlyProperty(key); // NOTE: will also thorw this to nonexistent properties but eh, who cares?
    }

    protected override Object RequestSetValue(Object value) {
        this.value = value.ToStr().value;
        return value;
    }

    public override Object Copy(bool shallow = true, params object[] keys) {
        return new Str(shallow ? value : new string(value));
    }

    public override Object Call(params Object[] args) {
        throw new NotSupportedException();
    }

    public override Object ExecuteBinary(OperatorType @operator, Object rhs) {
        if (@operator == OperatorType.Equal)
            return new Bool(this.value.Equals(rhs.Value));
        else if (@operator == OperatorType.Equal)
            return new Bool(!this.value.Equals(rhs.Value));

        string value = rhs.ToStr().value;
        return @operator switch {
            OperatorType.Equal => new Bool(value.Equals(rhs.Value)),
            OperatorType.NotEqual => new Bool(!value.Equals(rhs.Value)),
            OperatorType.Plus => new Str(this.value + value),
            _ => throw NotSupportedOperator(@operator)
        };
    }

    public override Object ExecuteUnary(OperatorType @operator, bool prefix) {
        throw NotSupportedOperator(@operator);
    }

    public override IEnumerator<Object> GetEnumerator() {
        foreach (char c in value)
            yield return new Str(new(c, 1));
    }

    public override string ToString() => value;

    public override Array ToArray() {
        return new(value.Map(it => new Str(new(it, 1))));
    }

    public override Bool ToBool() {
        return new Bool(!string.IsNullOrEmpty(value));
    }
}