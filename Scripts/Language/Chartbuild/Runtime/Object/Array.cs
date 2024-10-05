using System;
using System.Collections.Generic;

namespace PCE.Chartbuild.Runtime;

public class Array : Object {
    public List<Object> content;
    public override object Value => content;

    public override Object this[object key] {
        get => key switch {
            int idx => content[idx],
            "length" => throw new NotImplementedException(),
            _ => throw KeyNotFound(key),
        };
        set {
            switch (key) {
                case int idx:
                    content[idx] = value;
                    break;
                case "length":
                    throw ReadOnlyProperty(key);
                default:
                    throw new KeyNotFoundException();
            }
        }
    }

    public Array() {
        content = [];
    }

    public Array(IEnumerable<Object> objects) {
        content = new(objects);
    }

    public override Object Copy(bool shallow = true, params object[] keys) {
        List<Object> copy = new(content.Count);
        copy.AddRange(content.Map(it => it.Copy(shallow)));

        foreach (object obj in keys) {
            int idx = (int)obj;
            copy[idx] = content[idx].Copy(!shallow);
        }

        return new Array(copy);
    }

    public override Object Call(params Object[] args) {
        throw NotCallable();
    }

    public override Object ExecuteBinary(OperatorType @operator, Object rhs) {
        return @operator switch {
            OperatorType.Equal => new Bool(content.Equals(rhs.Value)),
            OperatorType.NotEqual => new Bool(!content.Equals(rhs.Value)),
            _ => throw NotSupportedOperator(@operator)
        };
    }

    public override Object ExecuteUnary(OperatorType @operator, bool prefix) {
        throw NotSupportedOperator(@operator);
    }

    public override IEnumerator<Object> GetEnumerator() => content.GetEnumerator();

    public override string ToString() => $"[{string.Join(", ", content)}]";

    public override Array ToArray() {
        return this;
    }

    public override Bool ToBool() {
        return new(content.Count > 0);
    }
}