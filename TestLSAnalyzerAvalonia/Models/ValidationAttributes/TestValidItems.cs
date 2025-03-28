using System.ComponentModel.DataAnnotations;
using LSAnalyzerAvalonia.Models.ValidationAttributes;

namespace TestLSAnalyzerAvalonia.Models.ValidationAttributes;

public class TestValidItems
{
    [Fact]
    public void TestIsValidInSpecialCase()
    {
        ValidItems validator = new("invalid");
        Assert.True(validator.IsValid(null));
    }
}