using Godot;

namespace PCE.Editor;

public partial class ValueContainerGraphNode<T> : GraphNode {
    public virtual T Value { get; protected set; }

    public ValueContainerGraphNode() {
        Title = "value";
        Resizable = true;
    }
}