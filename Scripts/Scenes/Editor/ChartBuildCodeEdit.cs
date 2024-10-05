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
                Chartbuild.ASTRoot ast = new Chartbuild.Parser(tokens).Parse();
                new ASTWalker(ast)
                // default values
                .InsertValue(true, "true", new Bool(true))
                .InsertValue(true, "false", new Bool(false))
                .InsertValue(true, "unset", new Unset())
                .InsertValue(true, "chart", chart.ToObject())
                .InsertValue(true, "PLATFORM", new I32((int)chart.Platform)) // should be fine since it's not going to change
                .InsertValue(true, "PCE", new I32((int)CompatibilityLevel.PCE))
                .InsertValue(true, "RPE", new I32((int)CompatibilityLevel.RPE))
                .InsertValue(true, "PHI", new I32((int)CompatibilityLevel.PHI))
                // default constructors
                .InsertValue(true, "vec2", new NativeFunction(args => {
                    if (args.Length == 0)
                        return new Vec2(Vector2.Zero);
                    else if (args.Length == 1)
                        return args[0].ToVec2();
                    else
                        return new Vec2(new(args[0].ToF32().value, args[1].ToF32().value));
                }))
                // default functions
                .InsertValue(true, "dbg_print", new NativeFunction(new Action<Object[]>(args => {
                    GD.Print(string.Join<Object>(", ", args));
                })))
                .Evaluate();
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
