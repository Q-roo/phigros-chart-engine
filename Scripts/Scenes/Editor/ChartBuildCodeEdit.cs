using Godot;
using System;

namespace PCE.Editor;

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
                Chartbuild.ASTRoot ast = new Chartbuild.Parser(tokens).Parse();
                // ast = Chartbuild.Analyzer.Analyze(ast);
                // ast = new Chartbuild.Runtime.Interpreter(ast).Analyze();
                // GD.Print(Newtonsoft.Json.JsonConvert.SerializeObject(
                //     ast,
                //     Newtonsoft.Json.Formatting.Indented,
                //     new Newtonsoft.Json.JsonSerializerSettings() {
                //         PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects,
                //         ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                //     }
                // ));
                GD.Print(new Chartbuild.Runtime.UnsafeByteCodeGenerator().Generate(ast).Dump());
                GD.Print(new Chartbuild.Runtime.UnsafeByteCodeGenerator().Generate(ast).BuildVM().Run());
                // GD.Print("raw byte code");
                // GD.Print(string.Join("\n", new Chartbuild.Runtime.UnsafeByteCodeGenerator().Generate(ast).GetCode()));
                // GD.Print(string.Join("\n", new Chartbuild.Runtime.ByteCodeGenerator(ast).Generate()));
                // GD.Print(new Chartbuild.Runtime.ByteCodeGenerator(ast).CreateVM().Dump());
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
