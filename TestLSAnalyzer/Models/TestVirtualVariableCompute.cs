using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using LSAnalyzer.Models;

namespace TestLSAnalyzer.Models;

public class TestVirtualVariableCompute
{
    private VirtualVariableComputeParser SetupParser(string text)
    {
        AntlrInputStream antlrInputStream = new(text);
        VirtualVariableComputeLexer lexer = new(antlrInputStream);
        CommonTokenStream commonTokenStream = new(lexer);
        VirtualVariableComputeParser parser = new(commonTokenStream);

        return parser;
    }

    [Theory, MemberData(nameof(TestVirtualVariableComputeParserData))]
    public void TestVirtualVariableComputeParser(string text, bool rootCorrect, bool termStructureCorrect, int expectedChildren, int firstTerminalNodeLexerType)
    {
        var parser = SetupParser(text);
        var expression = parser.expression();
        
        Assert.Equal(rootCorrect, expression.children.All(child => child is not ErrorNodeImpl));
        
        if (!rootCorrect) return;
        
        var rootTerm = expression.GetChild(0) as VirtualVariableComputeParser.TermContext;
        Assert.Equal(!termStructureCorrect, VirtualVariableCompute.TermHasErrorRecursive(rootTerm));
        
        if (!termStructureCorrect) return;
        
        Assert.Equal(expectedChildren, rootTerm!.ChildCount);
        
        if (rootTerm.GetChild(0) is not TerminalNodeImpl terminalNode) return;
        
        Assert.Equal(firstTerminalNodeLexerType, terminalNode.Symbol.Type);
    }

    public static IEnumerable<object[]> TestVirtualVariableComputeParserData => [
        [ "", true, false, 0, 0 ],
        [ "0.000", true, true, 1, VirtualVariableComputeLexer.NUMBER ],
        [ "abc", true, true, 1, VirtualVariableComputeLexer.VARIABLE ],
        [ "ABC", true, true, 1, VirtualVariableComputeLexer.VARIABLE ],
        [ "1abc", false, false, 1, 0 ],
        [ "abc -", true, false, 2, 0 ],
        [ "item2d + 2", true, true, 3, VirtualVariableComputeLexer.VARIABLE ],
        [ "-0.25 + item12 / 12.2", true, true, 3, VirtualVariableComputeLexer.NUMBER ],
    ];

    [Theory, MemberData(nameof(TestVirtualVariableComputeParserData))]
    public void TestIsValid(string text, bool rootCorrect, bool termStructureCorrect, int expectedChildren, int firstTerminalNodeLexerType)
    {
        VirtualVariableCompute virtualVariableCompute = new()
        {
            Expression = text
        };

        Assert.Equal(rootCorrect && termStructureCorrect, virtualVariableCompute.IsValid);
    }

    [Theory, MemberData(nameof(TestFromPlausibleValuesData))]
    public void TestFromPlausibleValues(string text, bool expected)
    {
        VirtualVariableCompute virtualVariableCompute = new()
        {
            Expression = text,
            PossiblePlausibleValueVariables = [
                new PlausibleValueVariable { DisplayName = "MATH", Regex = "PV[0-9]+MATH", Label = "PV in Maths", Mandatory = false }
            ]
        };
        
        Assert.Equal(expected, virtualVariableCompute.FromPlausibleValues);
    }

    public static IEnumerable<object[]> TestFromPlausibleValuesData => [
        [ "-", false ],
        [ "4 + abc", false ],
        [ "MATH", true ],
        [ "4 + MATH", true ],
        [ "MATH + SCIE", true ],
        [ "-3.4 + SCIE - MATH", true ]
    ];
}