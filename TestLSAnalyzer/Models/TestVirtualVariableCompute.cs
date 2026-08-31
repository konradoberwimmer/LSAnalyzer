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
        [ "isNA(item1)", true, 0 ],
        [ "isNa(item1)", false, 4 ],
        [ "isNA(item1 == 2)", true, 0 ],
        [ "2 * isNA(item1 + item2 + item3)", true, 0 ],
        [ "!", false, 1 ],
        [ "!(x == 1)", true, 0 ],
        [ "!isNA(item1)", true, 0 ],
        [ "!isNA(item1) & item1 == 2", true, 0 ],
        [ "item1==1 |item1 == 2", true, 0 ],
        [ "rowSums(rmNA=T)", false, 12 ],
        [ "rowSums(item1, item2, item3)", true, 0 ],
        [ "rowSums(item1, item2, item3, rmNA = T)", true, 0 ],
        [ "rowSums(item1,item2,rmNA=)", false, 25 ],
        [ "RowMeans(item1,item2,item3)", false, 8 ],
        [ "rowSums(item1, rowMeans(item2, item3) * 2.0)", true, 0 ],
        [ "rowMeans(item1, rowMeans(item2, item3) * 2.0)", true, 0 ],
        [ "factorScores(item1, rowMeans(item2, item3) * 2.0)", true, 0 ],
        [ "linear(pv1)", true, 0 ],
        [ "linear(pv1, mean=100, sd = 0.25)", true, 0 ],
        [ "linear(pv1, mean=-100, sd = -0.25)", true, 0 ],
        [ "linear(pv1, mean=NA, sd = NA)", false, 17 ],
        [ "linear(pv1, mean=100, sd = 0.25, unnecessary = 12)", true, 0 ],
        [ "scale(pv1, mean=100, sd = 0.25, unnecessary = 12)", true, 0 ],
        [ "linear(pv1)", true, 0 ],
        [ "logarithmic(pv1)", true, 0 ],
        [ "logarithmic(pv1, center = F)", true, 0 ],
        [ "logarithmic(pv1, center = F, other = T)", false, 27 ],
        [ "logarithmic(pv1, center = F, logbase = 2)", false, 27 ], // TODO order of named parameters should not matter
        [ "logarithmic(pv1, logbase = 2, center = T)", true, 0 ],
        [ "recode(item1)", false, 12 ],
        [ "recode('else=copy')", false, 7 ],
        [ "recode(item1, else=copy)", false, 23 ],
        [ "recode(item1, 'else=copy')", true, 0 ],
        [ "recode(item1, item2, 'else=copy')", false, 14 ],
        [ "recode([ item1, item2 ], ' else=copy ')", true, 0 ],
        [ "recode([ item1, item2 ], '<=4=1;>=5=0; else=copy ')", true, 0 ], // syntactically correct even though invalid
        [ "recode([ item1, item2 ], '[<=4,NA]=1;[>=5,1]=0; else=NA ')", true, 0 ],
        [ "recode(item1, '1-4=1;5=0;else=NA')", true, 0 ],
        [ "recode(item1, '1-2=-1;3=0;4-5=1;else=NA')", true, 0 ],
        [ "recode(item1, '<=-1=0;-2--1=-1;-1-0=2;else=NA')", true, 0 ],
        [ "recode(item1, '')", true, 0 ],
        [ "recode(item1, '1-2=0;3-5=1')", true, 0 ],
        [ "sum(item1, item2)", false, 9 ],
        [ "sum(item1)", true, 0 ],
        [ "item1 - mean(item1)", true, 0 ],
        [ "(x - mean(x))/sd(x)", true, 0 ],
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
            Assert.Equal(position, virtualVariableCompute.LastSyntaxError?.CharPositionInLine);
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