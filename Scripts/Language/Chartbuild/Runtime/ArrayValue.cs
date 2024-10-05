using System;
using System.Collections;
using System.Collections.Generic;

namespace PCE.Chartbuild.Runtime;

public class ObjectValueArray : ObjectValue, IEnumerable<ObjectValue> {
    private readonly List<ObjectValue> values;
    public ObjectValueArray(List<ObjectValue> content)
    : base(content) {
        Type = ValueType.Array;
        values = content;
    }

    public override CBObject GetMember(object key) {
        return key switch {
            "length" => new(values.Count),
            int idx => new(values[idx]),
            _ => throw new MemberAccessException($"{key}")
        };
    }

    public override void SetMember(object key, CBObject value) {
        switch (key) {
            case int idx:
                values[idx] = value.GetValue();
                break;
            default:
                throw new MemberAccessException($"{key}");
        }
    }

    public IEnumerator<ObjectValue> GetEnumerator() => values.GetEnumerator();

    public override string ToString() {
        return $"[{string.Join(", ", values)}]";
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}