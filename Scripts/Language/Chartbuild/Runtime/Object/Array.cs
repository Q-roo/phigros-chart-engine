using System.Collections.Generic;
using System.Diagnostics;

namespace PCE.Chartbuild.Runtime;

public class Array : Object<List<Object>> {
    private readonly ReadOnlyProperty _length;
    private readonly ReadOnlyValueProperty _pushBack;
    private readonly ReadOnlyValueProperty _popFront;
    private readonly ReadOnlyValueProperty _extend;

    public Array(List<Object> list)
    : base(list) {
        _length = new(this, "length", (_, key) => {
            Debug.Assert(key.Equals("length"));
            return Value.Count;
        });

        _pushBack = new(this, "push_front", new Callable(args => Value.Add(args.Length > 0 ? args[0] : new Unset())));
        _popFront = new(this, "pop_pack", new Callable(_ => {
            Object ret = Value[^1];
            Value.RemoveAt(Value.Count - 1);
            return ret;
        }));
        _extend = new(this, "extend", new Callable(Value.AddRange));
    }

    public Array(IEnumerable<Object> content)
    : this(new(content)) { }

    public Array()
    : this([]) { }

    public override Property GetProperty(object key) => key switch {
        // should always be the idx from the switch
        int idx => new SetGetProperty(this, idx, (_, idx) => Value[(int)idx], (_, idx, value) => Value[(int)idx] = value),
        "length" => _length,
        "push_back" => _pushBack,
        "pop_front" => _popFront,
        "extend" => _extend,
        _ => base.GetProperty(key)
    };

    public override List<Object> ToList() => Value;
    public override bool ToBool() => Value is not null && Value.Count > 0;
    public override string ToString() => $"[{string.Join(", ", Value)}]";

    public override IEnumerator<Object> GetEnumerator() => Value.GetEnumerator();

    public override Object Copy(bool shallow = true, params object[] keys) {
        List<Object> ret = new(Value.Map(it => it.Copy(shallow)));

        // the keys might not be ordered
        foreach (object obj in keys) {
            int idx = (int)obj;
            ret[idx] = Value[idx].Copy(!shallow);
        }

        return ret;
    }
}