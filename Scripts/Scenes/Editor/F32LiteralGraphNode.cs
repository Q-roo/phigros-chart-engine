namespace PCE.Editor;

public partial class F32LiteralGraphNode : NumberLiteralGraphNode<float> {
    public F32LiteralGraphNode() {
        literal.Step = 0;
        literal.CustomArrowStep = 1;
    }
}