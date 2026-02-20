using System.Collections.Generic;
using LSAnalyzer.Models;
using RDotNet;

namespace LSAnalyzer.Services;

public interface IRservice
{
    public (string rHome, string rPath) RLocation { get; set; }
    
    public bool Connect();

    public bool IsConnected { get; }

    public string? GetRVersion();

    public bool CheckNecessaryRPackages(string? packageName = null);

    public bool InstallNecessaryRPackages(string? packageName = null);
    
    public bool NecessaryPackagesConfirmed { get; }

    public string? GetBifieSurveyVersion();

    public enum UpdateResult { Unavailable, Success, Failure }

    public UpdateResult UpdateBifieSurvey();

    public bool InjectAppFunctions();

    public bool LoadFileIntoGlobalEnvironment(string fileName, string? fileType = null);

    public bool SortRawDataStored(string sortBy);

    public bool ReplaceCharacterVariables();

    public SubsettingInformation TestSubsetting(string subsettingExpression, string? MIvar = null);

    public bool ApplySubsetting(string subsettingExpression);

    public bool ReduceToNecessaryVariables(List<string> regexNecessaryVariables, string? subsettingExpression = null);

    public bool ReduceToNecessaryVariables(Analysis analysis, List<string>? additionalVariables = null, string? subsettingExpression = null);

    public bool CreateReplicateWeights(string weight, string jkzone, string jkrep, bool jkreverse);

    public bool CreateBIFIEdataObject(string weight, int nmi, string? mivar, ICollection<PlausibleValueVariable>? pvvars, string? repwgts, double? fayfac, bool autoEncapsulatePVvars = false);

    public bool TestAnalysisConfiguration(AnalysisConfiguration analysisConfiguration, string? subsettingExpression = null);

    public bool PrepareForAnalysis(Analysis analysis);

    public List<Variable>? GetCurrentDatasetVariables(AnalysisConfiguration analysisConfiguration, bool fromStoredRaw = false);

    public List<GenericVector>? CalculateUnivar(AnalysisUnivar analysis);

    public List<GenericVector>? CalculateMeanDiff(AnalysisMeanDiff analysis);

    public List<GenericVector>? CalculateFreq(AnalysisFreq analysis);

    public List<GenericVector>? CalculateBivariate(AnalysisFreq analysis);

    public List<GenericVector>? CalculatePercentiles(AnalysisPercentiles analysis);

    public List<GenericVector>? CalculateCorr(AnalysisCorr analysis);

    public List<GenericVector>? CalculateLinreg(AnalysisLinreg analysis);

    public List<GenericVector>? CalculateLogistReg(AnalysisLogistReg analysis);

    public bool CreateVirtualVariable(VirtualVariable virtualVariable, List<PlausibleValueVariable>? pvVars = null);
    
    public List<Variable>? GetDatasetVariables(string fileName, string? fileType = null);

    public DataFrame? GetValueLabels(string variable);

    public bool Execute(string rCode, bool oneLiner = false);

    public SymbolicExpression? Fetch(string objectName);

    public void SendUserInterrupt();
    
    public void Dispose();
}