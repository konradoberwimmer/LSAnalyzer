using LSAnalyzer.Models;

namespace TestLSAnalyzer.Models;

public class TestVirtualVariableCompute
{
    public static IEnumerable<object[]> TestValidExpressionData => [
        [ "", false, 0 ],
        [ "0.000", true, 0 ],
        [ "-2.40", true, 0 ],
        [ "abc", true, 0 ],
        [ "ab$c", false, 3 ],
        [ "ABC", true, 0 ],
        [ "1abc", false, 1 ],
        [ "abc -", false, 5 ],
        [ "-abc", true, 0 ],
        [ "-(3+4)", true, 0 ],
        [ "item2d + 2", true, 0 ],
        [ "-0.25 + item12 / 12.2", true, 0 ],
        [ "(-0.25 + item12 / 12.2", false, 22 ],
        [ "(-0.25 - item12) / 12.2", true, 0 ],
        [ "( -0.25 + item12 ) / 12.2", true, 0 ],
        [ "(i1 + i2 + i3) / 3", true, 0 ],
        [ "(i1 + (i2 + i3) * 2) / 5", true, 0 ],
        [ "x ^ y", true, 0 ],
        [ "^x", false, 0 ],
        [ "x ^ (y-1)", true, 0 ],
        [ "wgt * (rep==1) * 2.0", true, 0 ],
        [ "scale > 0.5", true, 0 ],
        [ "mean <= 0", true, 0 ],
        [ "isNa(item1)", true, 0 ],
        [ "isNA(item1)", false, 4 ],
        [ "isNa(item1 == 2)", true, 0 ],
        [ "2 * isNa(item1 + item2 + item3)", true, 0 ],
        [ "!", false, 1 ],
        [ "!(x == 1)", true, 0 ],
        [ "!isNa(item1)", true, 0 ],
        [ "!isNa(item1) & item1 == 2", true, 0 ],
        [ "item1==1 |item1 == 2", true, 0 ],
    ];

    [Theory, MemberData(nameof(TestValidExpressionData))]
    public void TestValidExpression(string text, bool correct, int position)
    {
        VirtualVariableCompute virtualVariableCompute = new()
        {
            Expression = text
        };

        Assert.Equal(correct, virtualVariableCompute.ValidExpression);
        if (!correct)
        {
            Assert.Equal(position, virtualVariableCompute.LastSyntaxErrors.Last().CharPositionInLine);
        }
    }

    [Theory, MemberData(nameof(TestValidExpressionData))]
    public void TestValidate(string text, bool correct, int _)
    {
        VirtualVariableCompute virtualVariableCompute = new()
        {
            Name = "computed",
            Expression = text
        };

        Assert.Equal(correct, virtualVariableCompute.Validate());
    }
    
    public static IEnumerable<object[]> TestVariablesData => [
        [ "-", new List<string>(), false ],
        [ "4 + abc", new List<string> { "abc" }, false ],
        [ "MATH", new List<string> { "MATH" }, true ],
        [ "4 + MATH", new List<string> { "MATH" }, true ],
        [ "MATH + SCIE", new List<string> { "MATH", "SCIE" }, true ],
        [ "-3.4 + SCIE - MATH", new List<string> { "SCIE", "MATH" }, true ],
        [ "-3.4 + SCIE - MATH - SCIE", new List<string> { "SCIE", "MATH" }, true ]
    ];
    
    [Theory, MemberData(nameof(TestVariablesData))]
    public void TestVariables(string text, List<string> expected, bool _)
    {
        VirtualVariableCompute virtualVariableCompute = new()
        {
            Name = "computed",
            Expression = text
        };

        if (virtualVariableCompute.ValidExpression)
        {
            Assert.Equal(expected, virtualVariableCompute.Variables);
        }
    }

    [Theory, MemberData(nameof(TestVariablesData))]
    public void TestFromPlausibleValues(string text, List<string> _, bool expected)
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

    [Fact]
    public void TestIsChanged()
    {
        VirtualVariableCompute virtualVariableCompute = new()
        {            
            Name = "computed",
            PossiblePlausibleValueVariables = [
                new PlausibleValueVariable { DisplayName = "MATH", Regex = "PV[0-9]+MATH", Label = "PV in Maths", Mandatory = false }
            ],
            Expression = "MATH + SCIE"
        };
        
        Assert.True(virtualVariableCompute.IsChanged);
        
        virtualVariableCompute.AcceptChanges();
        
        Assert.False(virtualVariableCompute.IsChanged);
    }
}