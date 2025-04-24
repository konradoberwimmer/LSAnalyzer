using System.Collections.Immutable;
using LSAnalyzerAvalonia.Models;

namespace TestLSAnalyzerAvalonia.Models;

public class TestDatasetTypeListExtensions
{
    [Theory, MemberData(nameof(TestGetCompatibleDatasetTypesData))]
    public void TestGetCompatibleDatasetTypes(List<DatasetType> datasetTypes, ImmutableList<string> columnNames,
        List<int> expectedPositions, List<int> expectedPriorities)
    {
        var result = datasetTypes.GetCompatibleDatasetTypes(columnNames);

        Assert.Equal(result.Select(res => res.priority), expectedPriorities);

        for (var i = 0; i < datasetTypes.Count; i++)
        {
            if (expectedPositions.Contains(i))
            {
                Assert.Contains(datasetTypes[i], result.Select(res => res.datasetType));
            }
            else
            {
                Assert.DoesNotContain(datasetTypes[i], result.Select(res => res.datasetType));
            }
        }
    }

    public static TheoryData<List<DatasetType>, ImmutableList<string>, List<int>, List<int>> TestGetCompatibleDatasetTypesData {
        get
        {
            var data = new TheoryData<List<DatasetType>, ImmutableList<string>, List<int>, List<int>>();
            data.Add([], ["anything"], [], []);
            data.Add(DatasetType.CreateDefaultDatasetTypes(), [ "stupid_data" ], [], []);
            data.Add([ new DatasetType { } ], [ string.Empty ], [ 0 ], [ 0 ]);
            data.Add([ new DatasetType { Weight = "wgtstud" }, new DatasetType { Weight = "wgtclass" }], [ "id", "wgtclass" ], [ 1 ], [ 0 ]);
            data.Add([ new DatasetType { Weight = "wgtstud", IDvar = "idstud" }, new DatasetType { Weight = "wgtstud", IDvar = "id" }], [ "idstud", "wgtstud" ], [ 0 ], [ 0 ]);
            data.Add([ new DatasetType { Weight = "wgtstud", MIvar = "micnt" }, new DatasetType { Weight = "wgtstud", MIvar = "impnr" }], [ "impnr", "wgtstud" ], [ 1 ], [ 0 ]);
            data.Add([ 
                new DatasetType { NMI = 3, PVvarsList = [ new PlausibleValueVariable { Regex = "ASRREA", Mandatory = true } ] },
                new DatasetType { NMI = 5, PVvarsList = [ new PlausibleValueVariable { Regex = "ASRREA", Mandatory = true } ] },
                new DatasetType { NMI = 3, AutoEncapsulateRegex = false, PVvarsList = [ new PlausibleValueVariable { Regex = "SRREA", Mandatory = true } ] },
                new DatasetType { NMI = 3, AutoEncapsulateRegex = true, PVvarsList = [ new PlausibleValueVariable { Regex = "SRREA", Mandatory = true } ] },
                new DatasetType { NMI = 5, PVvarsList = [ new PlausibleValueVariable { Regex = "ASSSCI", Mandatory = false } ] },
            ], [ string.Empty, "ASRREA01", "ASRREA02", "ASRREA03" ], [ 0, 2, 4 ], [ 0, 0, 0 ]);
            data.Add([
                new DatasetType { AutoEncapsulateRegex = true, RepWgts = "epwgt" },
                new DatasetType { AutoEncapsulateRegex = false, RepWgts = "epwgt" },
                new DatasetType { },
            ], [ string.Empty, "repwgt1", "repwgt2", "repwgt3", "repwgt4" ], [ 1, 2 ], [ 1, 0 ]);
            data.Add([
                new DatasetType { JKzone = "jk_zone", JKrep = "JKREP" },
                new DatasetType { JKzone = "JKZONE", JKrep = "jk_rep" },
                new DatasetType { JKzone = "JKZONE", JKrep = "JKREP" },
                new DatasetType { JKzone = "JKZONE" },
            ], [ string.Empty, "JKZONE", "JKREP" ], [ 2, 3 ], [ 0, 0 ]);
            return data;
        }
    }
}