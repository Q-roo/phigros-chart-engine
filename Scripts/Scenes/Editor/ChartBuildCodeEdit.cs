using Godot;
using PCE.Chart;
using PCE.Chartbuild.Bindings;
using PCE.Chartbuild.Runtime;
using System;

namespace PCE.Editor;

using Object = Chartbuild.Runtime.Object;

public partial class ChartBuildCodeEdit : CodeEdit {
    [GetNode("./VBoxContainer/Toolbar/HBoxContainer/Run")] private Button runButton;
    // private static readonly Color stringColor = new(0xCE9178FF);
    // private static readonly Color numberColor = new(0xB5CEA8FF);
    // private static readonly Color functionColor = new(0xDCDCAAFF);
    // private static readonly Color symbolColor = new(0x2f2f2fda);
    // private static readonly Color memberVariableColor = new(0x9cdcfe);
    // private static readonly Color controlFlowColor = new(0xC586C0FF);
    // private static readonly Color keywordColor = new(0x569CD6FF);
    // private static readonly Color commentColor = new(0x6A9955FF);

    private CodeHighlighter CodeHighlighter {
        get => (CodeHighlighter)SyntaxHighlighter;
        set => SyntaxHighlighter = value;
    }

    // private static readonly string[] controlFlowKeywords = [
    //     "if",
    //     "else",
    //     "switch",
    //     "case",
    //     "for",
    //     "while",
    //     "in",
    //     "break",
    //     "continue",
    //     "return"
    // ];

    // private static readonly string[] keywords = [
    //     "fn",
    //     "let",
    //     "const"
    // ];

    public sealed override void _Ready() {
        runButton = GetNode<Button>("../Toolbar/HBoxContainer/Run");
        runButton.Pressed += () => {
            try {
                Chartbuild.BaseToken[] tokens = Chartbuild.Lexer.Parse(Text);
                GD.Print(tokens);

                Chart.Chart chart = GetNode<Chart.Chart>("../../../../../ChartRenderer");
                ChartContext.Reset();
                ChartContext.Init(chart);
                Chartbuild.ASTRoot ast = new Chartbuild.Parser(tokens).Parse();
                ASTWalker walker = new(ast);
                chart.Reset();
                walker
                // default values
                .InsertValue(true, "true", new Bool(true))
                .InsertValue(true, "false", new Bool(false))
                .InsertValue(true, "unset", new Unset())
                .InsertValue(true, "chart", chart.ToObject())
                .InsertProperty("PLATFORM", () => new I32((int)Chart.Chart.Platform))
                .InsertProperty("current_time_in_seconds", () => new F32((float)chart.CurrentTime)) // TODO: give this a shorter name
                .InsertProperty("delta_time_in_seconds", () => new F32((float)chart.DeltaTime)) // TODO: give this a shorter name
                .InsertValue(true, "PCE", new I32((int)CompatibilityLevel.PCE))
                .InsertValue(true, "RPE", new I32((int)CompatibilityLevel.RPE))
                .InsertValue(true, "PHI", new I32((int)CompatibilityLevel.PHI))
                // event trigger constructors
                .InsertProperty("begin", () => new OnChartBegin().ToObject())
                .InsertProperty("end", () => new OnChartEnd().ToObject())
                .InsertProperty("pause", () => new OnPause().ToObject())
                .InsertProperty("resume", () => new OnResume().ToObject())
                .InsertValue(true, "before", new NativeFunction(args => {
                    // signature: (float, ...rest)
                    return new OnTimeBefore(args[0].ToF32().value).ToObject();
                }))
                .InsertValue(true, "after", new NativeFunction(args => {
                    // signature: (float, ...rest)
                    return new OnTimeAfter(args[0].ToF32().value).ToObject();
                }))
                // TODO: touch events
                .InsertValue(true, "signal", new NativeFunction(args => {
                    // signature (str, ...rest)
                    return new OnSignal(args[0].ToStr().value).ToObject();
                }))
                .InsertValue(true, "delay", new NativeFunction(args => {
                    // signature (trigger, trigger, ...rest)
                    if (args.Length < 2)
                        throw new ArgumentException("insufficient arguments");

                    if (args[0].Value is not EventTrigger delay)
                        throw new ArgumentException("first argument needs to be an event trigger");

                    if (args[0].Value is not EventTrigger trigger)
                        throw new ArgumentException("first argument needs to be an event trigger");

                    return new OnDelayed(delay, trigger).ToObject();
                }))
                .InsertValue(true, "condition", new NativeFunction(args => {
                    // signature: (() => bool, ...rest)
                    if (args.Length == 0)
                        throw new ArgumentException("first argument needs to be a callable that returns a bool value");

                    return new OnCondition(() => args[0].Call().ToBool().value).ToObject();
                }))
                // default constructors
                .InsertValue(true, "vec2", new NativeFunction(args => {
                    if (args.Length == 0)
                        return new Vec2(Vector2.Zero);
                    else if (args.Length == 1)
                        return args[0].ToVec2();
                    else
                        return new Vec2(new(args[0].ToF32().value, args[1].ToF32().value));
                }))
                .InsertValue(true, "judgeline", new NativeFunction(args => {
                    // signature: ()
                    switch (args.Length) {
                        case 0:
                            return new Judgeline(ChartContext.GetJudgelineName(), walker.CurrentScope.rules.DefaultJudgelineBpm, walker.CurrentScope.rules.DefaultJudgelineSize).ToObject();
                        // signature: (name) or (bpm)
                        case 1: {
                            if (args[0] is Str str)
                                return new Judgeline(str.value, walker.CurrentScope.rules.DefaultJudgelineBpm, walker.CurrentScope.rules.DefaultJudgelineSize).ToObject();
                            else
                                return new Judgeline(ChartContext.GetJudgelineName(), args[0].ToF32().value, walker.CurrentScope.rules.DefaultJudgelineSize).ToObject();
                        }
                        // signature (name, bpm) or (bpm, size)
                        case 2: {
                            if (args[0] is Str str)
                                return new Judgeline(str.value, args[1].ToF32().value, walker.CurrentScope.rules.DefaultJudgelineSize).ToObject();
                            else
                                return new Judgeline(ChartContext.GetJudgelineName(), args[0].ToF32().value, args[1].ToF32().value).ToObject();
                        }
                        // signature(name, bpm, size, ...rest)
                        default:
                            return new Judgeline(args[0].ToStr().value, args[1].ToF32().value, args[2].ToF32().value).ToObject();
                    }
                }))
                .InsertValue(true, "event", new NativeFunction(args => {
                    // signature: (trigger | number, trigger | number, callback, ...rest)
                    if (args.Length < 3)
                        throw new ArgumentException("insufficient arguments");

                    if (args[0].Value is not EventTrigger start)
                        start = new OnTimeAfter(args[0].ToF32().value);

                    if (args[1].Value is not EventTrigger end)
                        end = new OnTimeBefore(args[1].ToF32().value);

                    return new Event(start, end, @this => {
                        args[2].Call(@this);
                    }).ToObject();
                }))
                // default functions
                .InsertValue(true, "dbg_print", new NativeFunction(new Action<Object[]>(args => {
                    GD.Print(string.Join<Object>(", ", args));
                })))
                .InsertValue(true, "emit", new NativeFunction(args => {
                    // signature: (str, ...rest)
                    chart.signals.Add(args[0].ToStr().value);
                }))
                .Evaluate();
                chart.BeginRender();
            } catch (Exception ex) {
                GD.Print(ex);
            }
        };
        // Already defined in the code highlighter resource
        // CodeHighlighter.NumberColor = numberColor;
        // CodeHighlighter.FunctionColor = functionColor;
        // CodeHighlighter.SymbolColor = symbolColor;
        // CodeHighlighter.MemberVariableColor = memberVariableColor;

        // foreach (string controlFlowKeyword in controlFlowKeywords)
        // {
        //     CodeHighlighter.AddKeywordColor(controlFlowKeyword, controlFlowColor);
        // }

        // foreach (string keyword in keywords)
        // {
        //     CodeHighlighter.AddMemberKeywordColor(keyword, keywordColor);
        // }

        // CodeHighlighter.AddColorRegion("'", "'", stringColor);
        // CodeHighlighter.AddColorRegion("\"", "\"", stringColor);
        // CodeHighlighter.AddColorRegion("/*", "*/", commentColor);
        // CodeHighlighter.AddColorRegion("//", string.Empty, commentColor);
    }

    public void Open(Project project) {
        using FileAccess file = FileAccess.Open(project.chartPath, FileAccess.ModeFlags.Read);
        if (file is null)
            OS.Alert(FileAccess.GetOpenError().ToString(), "Failed to open chartfile for reading");
        else
            Text = file.GetAsText(true);
    }

    public void Save(Project project) {
        using FileAccess file = FileAccess.Open(project.chartPath, FileAccess.ModeFlags.Write);
        if (file is null)
            OS.Alert(FileAccess.GetOpenError().ToString(), "Failed to open chartfile for writing");
        else
            file.StoreString(Text);
    }

    public void Close() {
        Text = string.Empty;
    }
}
