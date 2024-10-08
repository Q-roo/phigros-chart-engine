using DotNext;
using Godot;

namespace PCE.Chartbuild.Runtime;

public class ScopeRules {
    // fallback values
    private const float fallbackJudgelineSize = 4000;
    private const float fallbackJudgelineBpm = 120;
    private const float fallbackAspectRatio = 16f/9f;
    private const float fallbackNoteSpeed = 1;
    private const bool fallbackIsNoteAbove = true;

    // rules
    private Optional<float> _defaultJudgelineSize;
    private Optional<float> _defaultJudgelineBpm;
    private Optional<float> _defaultNoteSpeed;
    private Optional<bool> _defaultIsNoteAbove;
    private float _aspectRatio;

    // rule getters
    public float DefaultJudgelineSize { get => _defaultJudgelineSize.Or(fallbackJudgelineSize); set => _defaultJudgelineSize = value; }
    public float DefaultJudgelineBpm { get => _defaultJudgelineBpm.Or(fallbackJudgelineBpm); set => _defaultJudgelineBpm = value; }
    public float DefaultNoteSpeed { get => _defaultNoteSpeed.Or(fallbackNoteSpeed); set => _defaultNoteSpeed = value; }
    public bool DefaultIsNoteAbove { get => _defaultIsNoteAbove.Or(fallbackIsNoteAbove); set => _defaultIsNoteAbove = value; }
    public float AspectRatio {
        get => ProjectSettings.GetSetting("display/window/stretch/scale").As<float>();
        set {
            ProjectSettings.SetSetting("display/window/stretch/scale", Variant.From(value));
            _aspectRatio = value;
        }
    }

    public ScopeRules() {
        _defaultJudgelineSize = new();
        _defaultJudgelineBpm = new();
        _defaultNoteSpeed = new();
        _defaultIsNoteAbove = new();
        _aspectRatio = fallbackAspectRatio;
        if (AspectRatio != fallbackAspectRatio)
            AspectRatio = fallbackAspectRatio;
    }

    public ScopeRules(ScopeRules parent) {
        _defaultJudgelineSize = parent._defaultJudgelineSize;
        _defaultJudgelineBpm = parent._defaultJudgelineBpm;
        _defaultNoteSpeed = parent._defaultNoteSpeed;
        _defaultIsNoteAbove = parent._defaultIsNoteAbove;
        _aspectRatio = parent._aspectRatio;
    }

    public void UpdateAspectRatio() {
        if (AspectRatio != _aspectRatio)
            AspectRatio = _aspectRatio;
    }
}