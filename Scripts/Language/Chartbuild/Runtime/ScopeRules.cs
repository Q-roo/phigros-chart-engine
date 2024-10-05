using DotNext;

namespace PCE.Chartbuild.Runtime;

public class ScopeRules {
    // fallback values
    private static readonly float fallbackJudgelineSize = 4000;
    private static readonly float fallbackJudgelineBpm = 120;

    // rules
    private Optional<float> _defaultJudgelineSize;
    private Optional<float> _defaultJudgelineBpm;

    // rule getters
    public float DefaultJudgelineSize { get => _defaultJudgelineSize.Or(fallbackJudgelineSize); set => _defaultJudgelineSize = value; }
    public float DefaultJudgelineBpm { get => _defaultJudgelineBpm.Or(fallbackJudgelineBpm); set => _defaultJudgelineBpm = value; }

    public ScopeRules() {
        _defaultJudgelineSize = new();
        _defaultJudgelineBpm = new();
    }

    public ScopeRules(ScopeRules parent) {
        _defaultJudgelineSize = parent._defaultJudgelineSize;
        _defaultJudgelineBpm = parent._defaultJudgelineBpm;
    }
}