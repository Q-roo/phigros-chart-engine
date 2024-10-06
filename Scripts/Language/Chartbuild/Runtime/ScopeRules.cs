using DotNext;
using Godot;

namespace PCE.Chartbuild.Runtime;

public class ScopeRules {
    // fallback values
    private static readonly float fallbackJudgelineSize = 4000;
    private static readonly float fallbackJudgelineBpm = 120;
    private static readonly float fallbackAspectRatio = 1.777778f;

    // rules
    private Optional<float> _defaultJudgelineSize;
    private Optional<float> _defaultJudgelineBpm;
    private float _aspectRatio;

    // rule getters
    public float DefaultJudgelineSize { get => _defaultJudgelineSize.Or(fallbackJudgelineSize); set => _defaultJudgelineSize = value; }
    public float DefaultJudgelineBpm { get => _defaultJudgelineBpm.Or(fallbackJudgelineBpm); set => _defaultJudgelineBpm = value; }
    public float AspectRatio { get => ProjectSettings.GetSetting("display/window/stretch/scale").As<float>(); set {
        ProjectSettings.SetSetting("display/window/stretch/scale", Variant.From(value));
        _aspectRatio = value;
    } }

    public ScopeRules() {
        _defaultJudgelineSize = new();
        _defaultJudgelineBpm = new();
        _aspectRatio = fallbackAspectRatio;
        if (AspectRatio != fallbackAspectRatio)
        AspectRatio = fallbackAspectRatio;
    }

    public ScopeRules(ScopeRules parent) {
        _defaultJudgelineSize = parent._defaultJudgelineSize;
        _defaultJudgelineBpm = parent._defaultJudgelineBpm;
        _aspectRatio = parent._aspectRatio;
    }

    public void UpdateAspectRatio() {
        if (AspectRatio != _aspectRatio)
        AspectRatio = _aspectRatio;
    }
}