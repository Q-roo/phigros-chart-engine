using System.Collections.Generic;

namespace PCE.Chartbuild.Runtime;

public interface IEnumerableICBValue : ICBValue, IEnumerable<ICBValue> {
    public BaseType InnerType { get; }
}