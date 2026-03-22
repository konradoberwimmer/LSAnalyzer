using System.Collections.Generic;
using System.Data;
using LSAnalyzer.Models;
using RDotNet;

namespace LSAnalyzer.Services.Stubs;

public class RserviceStub : IRservice
{
    public (string rHome, string rPath) RLocation { get; set; } = (string.Empty, string.Empty);
    
    public bool Connect()
    {
        return false;
    }

    public bool IsConnected => false;

    public bool NecessaryPackagesConfirmed => false;

    public string? GetRVersion()
    {
        return null;
    }
    public bool CheckNecessaryRPackages(string? packageName = null)
    {
        throw new System.NotImplementedException();
    }

    public bool InstallNecessaryRPackages(string? packageName = null)
    {
        throw new System.NotImplementedException();
    }

    public string? GetBifieSurveyVersion()
    {
        return null;
    }

    public IRservice.UpdateResult UpdateBifieSurvey()
    {
        throw new System.NotImplementedException();
    }

    public bool InjectAppFunctions()
    {
        throw new System.NotImplementedException();
    }

    public bool LoadFileIntoGlobalEnvironment(string fileName, string? fileType = null)
    {
        throw new System.NotImplementedException();
    }

    public bool SortRawDataStored(string sortBy)
    {
        throw new System.NotImplementedException();
    }

    public bool ReplaceCharacterVariables()
    {
        throw new System.NotImplementedException();
    }

    public SubsettingInformation TestSubsetting(string subsettingExpression, string? MIvar = null)
    {
        throw new System.NotImplementedException();
    }

    public bool ApplySubsetting(string subsettingExpression)
    {
        throw new System.NotImplementedException();
    }

    public bool ReduceToNecessaryVariables(List<string> regexNecessaryVariables, string? subsettingExpression = null)
    {
        throw new System.NotImplementedException();
    }

    public bool ReduceToNecessaryVariables(Analysis analysis, string? subsettingExpression = null)
    {
        throw new System.NotImplementedException();
    }

    public bool CreateReplicateWeights(string weight, string jkzone, string jkrep, bool jkreverse)
    {
        throw new System.NotImplementedException();
    }

    public bool CreateBIFIEdataObject(string weight, int nmi, string? mivar, ICollection<PlausibleValueVariable>? pvvars, string? repwgts, double? fayfac,
        bool autoEncapsulatePVvars = false)
    {
        throw new System.NotImplementedException();
    }

    public bool TestAnalysisConfiguration(AnalysisConfiguration analysisConfiguration, List<VirtualVariable> virtualVariables, string? subsettingExpression = null)
    {
        throw new System.NotImplementedException();
    }

    public bool PrepareForAnalysis(Analysis analysis)
    {
        throw new System.NotImplementedException();
    }

    public List<Variable>? GetCurrentDatasetVariables(AnalysisConfiguration analysisConfiguration, List<VirtualVariable> virtualVariables, bool fromStoredRaw = false)
    {
        throw new System.NotImplementedException();
    }

    public List<GenericVector>? CalculateUnivar(AnalysisUnivar analysis)
    {
        throw new System.NotImplementedException();
    }

    public List<GenericVector>? CalculateMeanDiff(AnalysisMeanDiff analysis)
    {
        throw new System.NotImplementedException();
    }

    public List<GenericVector>? CalculateFreq(AnalysisFreq analysis)
    {
        throw new System.NotImplementedException();
    }

    public List<GenericVector>? CalculateBivariate(AnalysisFreq analysis)
    {
        throw new System.NotImplementedException();
    }

    public List<GenericVector>? CalculatePercentiles(AnalysisPercentiles analysis)
    {
        throw new System.NotImplementedException();
    }

    public List<GenericVector>? CalculateCorr(AnalysisCorr analysis)
    {
        throw new System.NotImplementedException();
    }

    public List<GenericVector>? CalculateLinreg(AnalysisLinreg analysis)
    {
        throw new System.NotImplementedException();
    }

    public List<GenericVector>? CalculateLogistReg(AnalysisLogistReg analysis)
    {
        throw new System.NotImplementedException();
    }

    public bool CreateVirtualVariable(VirtualVariable virtualVariable, List<PlausibleValueVariable>? pvVars = null, bool forPreview = false)
    {
        throw new System.NotImplementedException();
    }

    public (bool success, DataTable? dataTable) GetPreviewData()
    {
        throw new System.NotImplementedException();
    }

    public List<Variable>? GetDatasetVariables(string fileName, string? fileType = null)
    {
        throw new System.NotImplementedException();
    }

    public DataFrame? GetValueLabels(string variable)
    {
        throw new System.NotImplementedException();
    }

    public bool Execute(string rCode, bool oneLiner = false)
    {
        throw new System.NotImplementedException();
    }

    public SymbolicExpression? Fetch(string objectName)
    {
        throw new System.NotImplementedException();
    }

    public void SendUserInterrupt()
    {
        throw new System.NotImplementedException();
    }

    public void Dispose()
    {
        throw new System.NotImplementedException();
    }
}