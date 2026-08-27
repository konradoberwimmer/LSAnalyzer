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

    public string? GetUserLibrary()
    {
        throw new System.InvalidOperationException();
    }

    public bool CheckNecessaryRPackages(string? packageName = null)
    {
        throw new System.InvalidOperationException();
    }

    public bool InstallNecessaryRPackages(string? packageName = null)
    {
        throw new System.InvalidOperationException();
    }

    public string? GetBifieSurveyVersion()
    {
        return null;
    }

    public IRservice.UpdateResult UpdateBifieSurvey()
    {
        throw new System.InvalidOperationException();
    }

    public bool TestLoadingBifieSurvey()
    {
        throw new System.InvalidOperationException();
    }

    public bool InjectAppFunctions(string[]? functionNames = null)
    {
        throw new System.InvalidOperationException();
    }

    public bool LoadFileIntoGlobalEnvironment(string fileName, string? fileType = null)
    {
        throw new System.InvalidOperationException();
    }

    public bool SortRawDataStored(string sortBy)
    {
        throw new System.InvalidOperationException();
    }

    public bool ReplaceCharacterVariables()
    {
        throw new System.InvalidOperationException();
    }

    public SubsettingInformation TestSubsetting(string subsettingExpression, string? MIvar = null)
    {
        throw new System.InvalidOperationException();
    }

    public bool ApplySubsetting(string subsettingExpression)
    {
        throw new System.InvalidOperationException();
    }

    public bool ReduceToNecessaryVariables(List<string> regexNecessaryVariables, string? subsettingExpression = null)
    {
        throw new System.InvalidOperationException();
    }

    public bool ReduceToNecessaryVariables(Analysis analysis, string? subsettingExpression = null)
    {
        throw new System.InvalidOperationException();
    }

    public bool CreateReplicateWeights(string weight, string jkzone, string jkrep, bool jkreverse)
    {
        throw new System.InvalidOperationException();
    }

    public bool CreateBIFIEdataObject(string weight, int nmi, string? mivar, ICollection<PlausibleValueVariable>? pvvars, string? repwgts, double? fayfac,
        bool autoEncapsulatePVvars = false)
    {
        throw new System.InvalidOperationException();
    }

    public bool TestAnalysisConfiguration(AnalysisConfiguration analysisConfiguration, List<VirtualVariable> virtualVariables, string? subsettingExpression = null)
    {
        throw new System.InvalidOperationException();
    }

    public bool PrepareForAnalysis(Analysis analysis)
    {
        throw new System.InvalidOperationException();
    }

    public List<Variable>? GetCurrentDatasetVariables(AnalysisConfiguration analysisConfiguration, List<VirtualVariable> virtualVariables, bool fromStoredRaw = false)
    {
        throw new System.InvalidOperationException();
    }

    public List<GenericVector>? CalculateUnivar(AnalysisUnivar analysis)
    {
        throw new System.InvalidOperationException();
    }

    public List<GenericVector>? CalculateMeanDiff(AnalysisMeanDiff analysis)
    {
        throw new System.InvalidOperationException();
    }

    public List<GenericVector>? CalculatePercDiff(AnalysisPercDiff analysis)
    {
        throw new System.InvalidOperationException();
    }

    public List<GenericVector>? CalculateFreq(AnalysisFreq analysis)
    {
        throw new System.InvalidOperationException();
    }

    public List<GenericVector>? CalculateBivariate(AnalysisFreq analysis)
    {
        throw new System.InvalidOperationException();
    }

    public List<GenericVector>? CalculatePercentiles(AnalysisPercentiles analysis)
    {
        throw new System.InvalidOperationException();
    }

    public List<GenericVector>? CalculateCorr(AnalysisCorr analysis)
    {
        throw new System.InvalidOperationException();
    }

    public List<GenericVector>? CalculateLinreg(AnalysisLinreg analysis)
    {
        throw new System.InvalidOperationException();
    }

    public List<GenericVector>? CalculateLogistReg(AnalysisLogistReg analysis)
    {
        throw new System.InvalidOperationException();
    }

    public bool CreateVirtualVariable(VirtualVariable virtualVariable, List<PlausibleValueVariable> pvVars, bool forPreview = false)
    {
        throw new System.InvalidOperationException();
    }

    public (bool success, DataTable? dataTable) GetPreviewData()
    {
        throw new System.InvalidOperationException();
    }

    public List<Variable>? GetDatasetVariables(string fileName, string? fileType = null, bool fromStoredRaw = false)
    {
        throw new System.InvalidOperationException();
    }

    public DataFrame? GetValueLabels(string variable)
    {
        throw new System.InvalidOperationException();
    }

    public List<double>? GetDistinctValues(Variable variable, List<PlausibleValueVariable> plausibleValueVariables)
    {
        throw new System.InvalidOperationException();
    }

    public List<double>? GetDistinctValues(Variable variable)
    {
        throw new System.InvalidOperationException();
    }

    public bool Execute(string rCode, bool oneLiner = false)
    {
        throw new System.InvalidOperationException();
    }

    public SymbolicExpression? Fetch(string objectName)
    {
        throw new System.InvalidOperationException();
    }

    public void SendUserInterrupt()
    {
        throw new System.InvalidOperationException();
    }

    public void ClearUserInterrupt()
    {
        throw new System.InvalidOperationException();
    }

    public void Dispose()
    {
        throw new System.InvalidOperationException();
    }
}