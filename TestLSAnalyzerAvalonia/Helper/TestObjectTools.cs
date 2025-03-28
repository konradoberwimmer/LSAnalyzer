using LSAnalyzerAvalonia.Helper;
using LSAnalyzerAvalonia.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzerAvalonia.Helper;

public class TestObjectTools
{
    [Fact]
    public void TestPublicInstancePropertiesEqual()
    {
        Assert.True(ObjectTools.PublicInstancePropertiesEqual((DatasetType?)null, (DatasetType?)null));
        
        DatasetType dst1 = new DatasetType();
        DatasetType dst2 = new DatasetType(dst1);

        Assert.True(ObjectTools.PublicInstancePropertiesEqual(dst1, dst2, new string[] { "PVvarsList", "Errors" }));

        dst1.FayFac = 2;

        Assert.False(ObjectTools.PublicInstancePropertiesEqual(dst1, dst2, new string[] { "PVvarsList", "Errors" }));

        dst2.FayFac = 2;

        Assert.True(ObjectTools.PublicInstancePropertiesEqual(dst1, dst2, new string[] { "PVvarsList", "Errors" }));
    }

    [Fact]
    public void TestDoesPropertyExist()
    {
        Assert.False(ObjectTools.DoesPropertyExist(new ExpandoObject(), "Errors"));
        Assert.True(ObjectTools.DoesPropertyExist(new DatasetType(), "PVvarsList"));
    }

    [Fact]
    public void TestElementObjectsEqual()
    {
        Collection<PlausibleValueVariable> collection1 = new();
        Collection<PlausibleValueVariable> collection2 = new();

        Assert.True(collection1.ElementObjectsEqual(collection2));

        collection1.Add(new PlausibleValueVariable());

        Assert.False(collection1.ElementObjectsEqual(collection2));

        collection2.Add(new PlausibleValueVariable());

        Assert.True(collection1.ElementObjectsEqual(collection2, "Errors"));

        collection1.First().Mandatory = true;

        Assert.False(collection1.ElementObjectsEqual(collection2, "Errors"));

        collection2.First().Mandatory = true;

        Assert.True(collection1.ElementObjectsEqual(collection2, "Errors"));
    }
}