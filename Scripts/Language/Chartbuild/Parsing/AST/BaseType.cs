using System.Collections.Generic;

namespace PCE.Chartbuild;

public abstract class BaseType
{
    public virtual BaseType parent => null;
    // returns the name
    public abstract override string ToString();

    public static bool operator ==(BaseType left, BaseType right) => left.ToString() == right.ToString();
    public static bool operator !=(BaseType left, BaseType right) => !(left == right);
    public override bool Equals(object obj) => obj is BaseType rigth && rigth == this;

    public override int GetHashCode() => ToString().GetHashCode();

    public virtual bool IsChildOf(BaseType parent)
    {
        BaseType p = this.parent;
        while (p is not null)
        {
            if (p == parent)
                return true;

            p = p.parent;
        }

        return false;
    }
}