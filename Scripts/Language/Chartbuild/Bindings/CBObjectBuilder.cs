using System;
using PCE.Chartbuild.Runtime;

namespace PCE.Chartbuild.Bindings;

public class CBObjectBuilder(object value) {
    private CBObject @object;
    private ObjectValue Value => @object.GetValue();

    public CBObjectBuilder CreateInstance() {
        @object = new(value);

        return this;
    }

    public CBObjectBuilder Addproperty(string name, FunctionalObjectPropertyDescriptor descriptor) {
        Value.SetMember(name, descriptor);
        return this;
    }

    public CBObjectBuilder AddFunction(string name, Func<CBObject[], CBObject> function) {
        Value.SetMember(name, new(function));
        return this;
    }

    public CBObjectBuilder AddFunction(string name, Action<CBObject[]> function) {
        return AddFunction(name, args => {
            function(args);
            return new(ObjectValue.Unset);
        });
    }

    public CBObject Build() => @object;
}