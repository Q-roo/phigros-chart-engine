using Godot;
using PCE.Chart;
using PCE.Chartbuild.Runtime;
using System;

namespace PCE.Editor;

using Callable = Chartbuild.Runtime.Callable;

public partial class ChartBuildCodeEdit : CodeEdit
{
    [GetNode("./VBoxContainer/Toolbar/HBoxContainer/Run")] private Button runButton;
    // private static readonly Color stringColor = new(0xCE9178FF);
    // private static readonly Color numberColor = new(0xB5CEA8FF);
    // private static readonly Color functionColor = new(0xDCDCAAFF);
    // private static readonly Color symbolColor = new(0x2f2f2fda);
    // private static readonly Color memberVariableColor = new(0x9cdcfe);
    // private static readonly Color controlFlowColor = new(0xC586C0FF);
    // private static readonly Color keywordColor = new(0x569CD6FF);
    // private static readonly Color commentColor = new(0x6A9955FF);

    private CodeHighlighter CodeHighlighter
    {
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

    public sealed override void _Ready()
    {
        runButton = GetNode<Button>("../Toolbar/HBoxContainer/Run");
        runButton.Pressed += () =>
        {
            try
            {
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
                .InsertValue(true, "true", true)
                .InsertValue(true, "false", false)
                .InsertValue(true, "unset", new U())
                .InsertValue(true, "chart", chart.ToObject())
                .InsertProperty("PLATFORM", () => (int)Chart.Chart.Platform)
                .InsertProperty("current_time_in_seconds", () => (float)chart.CurrentTime) // TODO: give this a shorter name
                .InsertProperty("delta_time_in_seconds", () => (float)chart.DeltaTime) // TODO: give this a shorter name
                .InsertValue(true, "PCE", (int)CompatibilityLevel.PCE)
                .InsertValue(true, "RPE", (int)CompatibilityLevel.RPE)
                .InsertValue(true, "PHI", (int)CompatibilityLevel.PHI)
                // event trigger constructors
                .InsertProperty("begin", () => new OnChartBegin().ToObject())
                .InsertProperty("end", () => new OnChartEnd().ToObject())
                .InsertProperty("pause", () => new OnPause().ToObject())
                .InsertProperty("resume", () => new OnResume().ToObject())
                .InsertValue(true, "before", new Callable(args =>
                {
                    // signature: (float, ...rest)
                    return new OnTimeBefore(args[0]).ToObject();
                }))
                .InsertValue(true, "after", new Callable(args =>
                {
                    // signature: (float, ...rest)
                    return new OnTimeAfter(args[0]).ToObject();
                }))
                .InsertValue(true, "exec", new Callable(args =>
                {
                    // singature: (int, ...rest)
                    return new OnExecCount(args[0]).ToObject();
                }))
                .InsertProperty("once", () => new OnExecCount(1).ToObject())
                // TODO: touch events
                .InsertValue(true, "signal", new Callable(args =>
                {
                    // signature (str, ...rest)
                    return new OnSignal(args[0].ToString()).ToObject();
                }))
                .InsertValue(true, "delay", new Callable(args =>
                {
                    // signature (trigger, trigger, ...rest)
                    if (args.Length < 2)
                        throw new ArgumentException("insufficient arguments");

                    if (args[0].NativeValue is not EventTrigger delay)
                        throw new ArgumentException("first argument needs to be an event trigger");

                    if (args[0].NativeValue is not EventTrigger trigger)
                        throw new ArgumentException("first argument needs to be an event trigger");

                    return new OnDelayed(delay, trigger).ToObject();
                }))
                .InsertValue(true, "condition", new Callable(args =>
                {
                    // signature: (() => bool, ...rest)
                    if (args.Length == 0)
                        throw new ArgumentException("first argument needs to be a callable that returns a bool value");

                    return new OnCondition(() => args[0].Call()).ToObject();
                }))
                // default constructors
                .InsertValue(true, "vec2", new Callable(args =>
                {
                    if (args.Length == 0)
                        return new V(Vector2.Zero);
                    else if (args.Length == 1)
                        return args[0].ToVec2();
                    else
                        return new V(new(args[0], args[1]));
                }))
                .InsertValue(true, "judgeline", new Callable(args =>
                {
                    // signature: ()
                    switch (args.Length)
                    {
                        case 0:
                            return new Judgeline(ChartContext.GetJudgelineName(), walker.CurrentScope.rules.DefaultJudgelineBpm, walker.CurrentScope.rules.DefaultJudgelineSize).ToObject();
                        // signature: (name) or (bpm)
                        case 1:
                            {
                                if (args[0] is S str)
                                    return new Judgeline(str.ToString(), walker.CurrentScope.rules.DefaultJudgelineBpm, walker.CurrentScope.rules.DefaultJudgelineSize).ToObject();
                                else
                                    return new Judgeline(ChartContext.GetJudgelineName(), args[0], walker.CurrentScope.rules.DefaultJudgelineSize).ToObject();
                            }
                        // signature (name, bpm) or (bpm, size)
                        case 2:
                            {
                                if (args[0] is S str)
                                    return new Judgeline(str.ToString(), args[1], walker.CurrentScope.rules.DefaultJudgelineSize).ToObject();
                                else
                                    return new Judgeline(ChartContext.GetJudgelineName(), args[0], args[1]).ToObject();
                            }
                        // signature(name, bpm, size, ...rest)
                        default:
                            return new Judgeline(args[0].ToString(), args[1], args[2]).ToObject();
                    }
                }))
                .InsertValue(true, "event", new Callable(args =>
                {
                    // signature: (trigger | number, trigger | number, callback, ...rest)
                    if (args.Length < 3)
                        throw new ArgumentException("insufficient arguments");

                    if (args[0].NativeValue is not EventTrigger start)
                        start = new OnTimeAfter(args[0]);

                    if (args[1].NativeValue is not EventTrigger end)
                        end = new OnTimeBefore(args[1]);

                    return new Event(start, end, @this =>
                    {
                        args[2].Call(@this);
                    }).ToObject();
                }))
                .InsertValue(true, "tap", new Callable(args =>
                {
                    return NoteConstructor(NoteType.Tap, walker.CurrentScope.rules.DefaultNoteSpeed, walker.CurrentScope.rules.DefaultIsNoteAbove, args).ToObject();
                }))
                .InsertValue(true, "drag", new Callable(args =>
                {
                    return NoteConstructor(NoteType.Drag, walker.CurrentScope.rules.DefaultNoteSpeed, walker.CurrentScope.rules.DefaultIsNoteAbove, args).ToObject();
                }))
                .InsertValue(true, "hold", new Callable(args =>
                {
                    return NoteConstructor(NoteType.Hold, walker.CurrentScope.rules.DefaultNoteSpeed, walker.CurrentScope.rules.DefaultIsNoteAbove, args).ToObject();
                }))
                .InsertValue(true, "flick", new Callable(args =>
                {
                    return NoteConstructor(NoteType.Flick, walker.CurrentScope.rules.DefaultNoteSpeed, walker.CurrentScope.rules.DefaultIsNoteAbove, args).ToObject();
                }))
                // default functions
                .InsertValue(true, "dbg_print", new Callable(args =>
                {
                    GD.Print(string.Join<O>(", ", args));
                }))
                .InsertValue(true, "emit", new Callable(args =>
                {
                    // signature: (str, ...rest)
                    chart.signals.Add(args[0].ToString());
                }));

                walker.Evaluate();
                chart.BeginRender();
            }
            catch (Exception ex)
            {
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

    private Note NoteConstructor(NoteType type, float defaultSpeed, bool defaultIsAbove, params O[] args)
    {
        // signature: (time, x_offset, speed=default, is_above=default, ...rest)


        float speed = defaultSpeed;
        bool isAbove = defaultIsAbove;
        bool isHold = type == NoteType.Hold;
        // if it is, then the signature changes
        // (time, hold_time, x_offset, speed=default, is_above=default, ...rest)
        int argOffset = isHold ? 1 : 0;
        if (args.Length < 2 + argOffset)
            throw new ArgumentException("insufficient arguments");

        if (args.Length == 3 + argOffset)
        {
            if (args[2 + argOffset] is B b)
                isAbove = b;
            else
                speed = args[2 + argOffset];
        }
        else if (args.Length > 3 + argOffset)
        {
            speed = args[3 + argOffset];
            isAbove = args[4 + argOffset];
        }

        return new Note(type, args[0], args[1 + argOffset], speed, isAbove, isHold ? args[1] : 0);
    }

    public void Open(Project project)
    {
        using FileAccess file = FileAccess.Open(project.chartPath, FileAccess.ModeFlags.Read);
        if (file is null)
            OS.Alert(FileAccess.GetOpenError().ToString(), "Failed to open chartfile for reading");
        else
            Text = file.GetAsText(true);
    }

    public void Save(Project project)
    {
        using FileAccess file = FileAccess.Open(project.chartPath, FileAccess.ModeFlags.Write);
        if (file is null)
            OS.Alert(FileAccess.GetOpenError().ToString(), "Failed to open chartfile for writing");
        else
            file.StoreString(Text);
    }

    public void Close()
    {
        Text = string.Empty;
    }
}
