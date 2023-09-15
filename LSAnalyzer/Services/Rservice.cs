using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using Microsoft.Win32;
using RDotNet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace LSAnalyzer.Services
{
    public class Rservice
    {
        private string? _rPath;
        private REngine? _engine;
        private readonly string[] _rPackages = new string[] { "BIFIEsurvey", "foreign" };

        public Rservice() 
        {
            var rPathObject = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\R-core\\R64", "InstallPath", null);
            if (rPathObject != null)
            {
                _rPath = rPathObject.ToString()!.Replace("\\", "/");
            }
        }

        public bool Connect()
        {
            if (_rPath == null)
            {
                return false;
            }

            try
            {
                _engine = REngine.GetInstance();
                _engine.ClearGlobalEnvironment();
                _engine.Evaluate("Sys.setenv(PATH = paste(\"" + _rPath + "/bin/x64\", Sys.getenv(\"PATH\"), sep=\";\"))"); //ugly workaround for now!
                string[] a = _engine.Evaluate("paste0('Result: ', stats::sd(c(1,2,3)))").AsCharacter().ToArray();
                if (a.Length == 0 || a[0] != "Result: 1")
                {
                    return false;
                }
            } catch
            {
                return false;
            }

            return true;
        }

        public bool CheckNecessaryRPackages()
        {
            try
            {
                foreach (string rPackage in _rPackages)
                {
                    bool available = _engine!.Evaluate("nzchar(system.file(package='" + rPackage + "'))").AsLogical().First();
                    if (!available)
                    {
                        return false;
                    }
                }

                return true;
            } catch 
            {
                return false;
            }
        }

        public bool InstallNecessaryRPackages()
        {
            try
            {
                bool userLibraryFolderConfigured = _engine!.Evaluate("nzchar(Sys.getenv('R_LIBS_USER'))").AsLogical().First();
                if (!userLibraryFolderConfigured)
                {
                    return false;
                }

                _engine.Evaluate("if (!dir.exists(Sys.getenv('R_LIBS_USER'))) { dir.create(Sys.getenv('R_LIBS_USER')) }");

                foreach (string rPackage in _rPackages)
                {
                    bool available = _engine.Evaluate("nzchar(system.file(package='" + rPackage + "'))").AsLogical().First();
                    if (!available)
                    {
                        try
                        {
                            _engine.Evaluate("install.packages('" + rPackage + "', lib = Sys.getenv('R_LIBS_USER'), repos = 'https://cloud.r-project.org')");
                        }
                        catch
                        {
                            return false;
                        }
                    }
                }

                return true;
            } catch
            {
                return false;
            }
        }

        public bool LoadFileIntoGlobalEnvironment(string fileName)
        {
            try
            {
                _engine!.Evaluate("lsanalyzer_dat_raw_stored <- foreign::read.spss('" + fileName.Replace("\\", "/") + "', use.value.labels = FALSE, to.data.frame = TRUE, use.missings = TRUE)");
                _engine.Evaluate("lsanalyzer_dat_raw <- lsanalyzer_dat_raw_stored");
                var rawData = _engine.GetSymbol("lsanalyzer_dat_raw").AsDataFrame();
                if (rawData == null)
                {
                    return false;
                }
            } catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public virtual SubsettingInformation TestSubsetting(string subsettingExpression, string? MIvar = null)
        {
            try
            {
                _engine!.Evaluate("lsanalyzer_dat_raw_stored_subset <- subset(lsanalyzer_dat_raw_stored, " + subsettingExpression + ")");

                int nCases;
                int nSubset;

                if (string.IsNullOrWhiteSpace(MIvar))
                {
                    _engine!.Evaluate("lsanalyzer_dat_raw_stored_ncases <- nrow(lsanalyzer_dat_raw_stored)");
                    nCases = _engine.GetSymbol("lsanalyzer_dat_raw_stored_ncases").AsInteger().First();

                    _engine.Evaluate("lsanalyzer_dat_raw_stored_nsubset <- nrow(lsanalyzer_dat_raw_stored_subset)");
                    nSubset = _engine.GetSymbol("lsanalyzer_dat_raw_stored_nsubset").AsInteger().First();
                } else
                {
                    _engine!.Evaluate("lsanalyzer_cnt_subset_mi <- table(lsanalyzer_dat_raw_stored_subset$" + MIvar + ")");
                    _engine!.Evaluate("lsanalyzer_cnt_subset_mi_max <- max(lsanalyzer_cnt_subset_mi)");
                    _engine!.Evaluate("lsanalyzer_cnt_subset_mi_all_equal <- all(lsanalyzer_cnt_subset_mi == lsanalyzer_cnt_subset_mi_max)");
                    var allMIEqual = _engine.GetSymbol("lsanalyzer_cnt_subset_mi_all_equal").AsLogical().First();
                    if (!allMIEqual)
                    {
                        return new SubsettingInformation() { ValidSubset = false, MIvariance = true };
                    }

                    _engine.Evaluate("lsanalyzer_dat_raw_stored_ncases_mi1 <- sum(!is.na(lsanalyzer_dat_raw_stored[, '" + MIvar + "']) & lsanalyzer_dat_raw_stored[, '" + MIvar + "'] == unique(lsanalyzer_dat_raw_stored[, '" + MIvar + "'])[1])");
                    nCases = _engine.GetSymbol("lsanalyzer_dat_raw_stored_ncases_mi1").AsInteger().First();

                    _engine.Evaluate("lsanalyzer_dat_raw_stored_nsubset_mi1 <- sum(!is.na(lsanalyzer_dat_raw_stored_subset[, '" + MIvar + "']) & lsanalyzer_dat_raw_stored_subset[, '" + MIvar + "'] == unique(lsanalyzer_dat_raw_stored_subset[, '" + MIvar + "'])[1])");
                    nSubset = _engine.GetSymbol("lsanalyzer_dat_raw_stored_nsubset_mi1").AsInteger().First();
                }

                _engine!.Evaluate("rm(lsanalyzer_dat_raw_stored_subset)");

                return new SubsettingInformation() { ValidSubset = true, NCases = nCases, NSubset = nSubset };
            } catch (Exception)
            {
                return new SubsettingInformation() { ValidSubset = false }; ;
            }
        }

        public bool ApplySubsetting(string subsettingExpression)
        {
            try
            {
                _engine!.Evaluate("lsanalyzer_dat_raw <- subset(lsanalyzer_dat_raw, " + subsettingExpression + ")");
                return true;
            } catch
            {
                return false;
            }
        }

        public bool ReduceToNecessaryVariables(List<string> regexNecessaryVariables, string? subsettingExpression = null)
        {
            try
            {
                _engine!.Evaluate("lsanalyzer_dat_raw <- lsanalyzer_dat_raw_stored");

                _engine.Evaluate("lsanalyzer_necessary_variables <- numeric(0)");
                foreach (string regexNecessaryVariable in regexNecessaryVariables)
                {
                    _engine.Evaluate("lsanalyzer_necessary_variable <- grep('" + regexNecessaryVariable + "', colnames(lsanalyzer_dat_raw))");
                    if (_engine.GetSymbol("lsanalyzer_necessary_variable").AsNumeric().Length == 0)
                    {
                        return false;
                    }
                    _engine.Evaluate("lsanalyzer_necessary_variables <- unique(c(lsanalyzer_necessary_variables, lsanalyzer_necessary_variable))");
                }

                if (!string.IsNullOrWhiteSpace(subsettingExpression) && !ApplySubsetting(subsettingExpression))
                {
                    return false;
                }

                _engine.Evaluate("lsanalyzer_dat_raw <- lsanalyzer_dat_raw[, lsanalyzer_necessary_variables]");
                var rawData = _engine.GetSymbol("lsanalyzer_dat_raw").AsDataFrame();
                if (rawData == null)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public bool ReduceToNecessaryVariables(Analysis analysis, List<string>? additionalVariables = null, string? subsettingExpression = null)
        {
            var regexNecessaryVariables = analysis.AnalysisConfiguration.GetRegexNecessaryVariables() ?? new();
            foreach (var variable in analysis.Vars)
            {
                regexNecessaryVariables.Add(variable.Name);
            }
            foreach (var variable in analysis.GroupBy)
            {
                regexNecessaryVariables.Add(variable.Name);
            }
            if (additionalVariables != null)
            {
                foreach (var variable in additionalVariables)
                {
                    regexNecessaryVariables.Add(variable);
                }
            }

            return ReduceToNecessaryVariables(regexNecessaryVariables, subsettingExpression);
        }

        public bool CreateReplicateWeights(int nrep, string weight, string jkzone, string jkrep, bool jkreverse)
        {
            try
            {
                _engine!.Evaluate("lsanalyzer_jk_zones <- sort(unique(lsanalyzer_dat_raw_stored[,'" + jkzone + "']))");
                _engine.Evaluate(
                    "for (lsanalyzer_jk_zone in lsanalyzer_jk_zones) " +
                    "   lsanalyzer_dat_raw[, paste0('lsanalyzer_repwgt_', lsanalyzer_jk_zone)] <- " +
                    "       lsanalyzer_dat_raw[,'" + weight + "'] * (lsanalyzer_dat_raw[, '" + jkzone + "'] != lsanalyzer_jk_zone) + " +
                    "       lsanalyzer_dat_raw[,'" + weight + "'] * (lsanalyzer_dat_raw[, '" + jkzone + "'] == lsanalyzer_jk_zone) * lsanalyzer_dat_raw[, '" + jkrep + "'] * 2;");
                if (jkreverse)
                {
                    _engine.Evaluate(
                        "for (lsanalyzer_jk_zone in lsanalyzer_jk_zones) " +
                        "   lsanalyzer_dat_raw[, paste0('lsanalyzer_repwgt_', lsanalyzer_jk_zone + max(lsanalyzer_jk_zones))] <- " +
                        "       lsanalyzer_dat_raw[,'" + weight + "'] * (lsanalyzer_dat_raw[, '" + jkzone + "'] != lsanalyzer_jk_zone) + " +
                        "       lsanalyzer_dat_raw[,'" + weight + "'] * (lsanalyzer_dat_raw[, '" + jkzone + "'] == lsanalyzer_jk_zone) * (1 - lsanalyzer_dat_raw[, '" + jkrep + "']) * 2;");
                }
                _engine.Evaluate("lsanalyzer_repwgts <- grep('lsanalyzer_repwgt_', colnames(lsanalyzer_dat_raw), value = TRUE);");
                var repWgts = _engine.GetSymbol("lsanalyzer_repwgts").AsCharacter();
                if (repWgts.Length != nrep)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public bool CreateBIFIEdataObject(string weight, int nmi, string? mivar, string? pvvars, int nrep, string? repwgts, double? fayfac)
        {
            try
            {
                string rawDataVariableName = "lsanalyzer_dat_raw";
                if (mivar != null && mivar.Length > 0)
                {
                    _engine!.Evaluate("lsanalyzer_dat_raw_list <- split(lsanalyzer_dat_raw, lsanalyzer_dat_raw[, '" + mivar + "'])");
                    rawDataVariableName = "lsanalyzer_dat_raw_list";
                }

                string baseCall = "lsanalyzer_dat_BO <- BIFIEsurvey::BIFIE.data(" + rawDataVariableName + ", wgt = '" + weight + "'";

                string repwgtArg = "";
                if (repwgts != null && repwgts.Length > 0)
                {
                    string repWgtsDataset = "lsanalyzer_dat_raw";
                    if (mivar != null && mivar.Length > 0)
                    {
                        repWgtsDataset = "lsanalyzer_dat_raw_list[[1]]";
                    }
                    repwgtArg = ", wgtrep = " + repWgtsDataset + "[, grep('" + repwgts + "', colnames(" + repWgtsDataset + "), value = TRUE)]";
                }

                string fayfacArg = "";
                if (fayfac != null)
                {
                    fayfacArg = ", fayfac = " + string.Format(CultureInfo.InvariantCulture, "{0:0.####}", fayfac);
                }

                string pvvarsArg = "";
                if (pvvars != null && pvvars.Length > 0)
                {
                    var pvvarsArray = pvvars.Split(";");
                    pvvarsArray = Array.ConvertAll(pvvarsArray, pvvar => "'" + pvvar + "'");
                    pvvarsArg = ", pv_vars = c(" + String.Join(", ", pvvarsArray) + ")";
                }

                string finalCall = baseCall + repwgtArg + fayfacArg + pvvarsArg + ", cdata = TRUE)";
                _engine!.Evaluate(finalCall);

                var bifieDataObject = _engine.GetSymbol("lsanalyzer_dat_BO").AsList();
                var nmiReported = (int)bifieDataObject["Nimp"].AsNumeric().First();
                var nrepReported = (int)bifieDataObject["RR"].AsNumeric().First();
                if (nmi != nmiReported || nrep != nrepReported)
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        public virtual bool TestAnalysisConfiguration(AnalysisConfiguration analysisConfiguration, string? subsettingExpression = null)
        {
            if (_engine == null || 
                analysisConfiguration.FileName == null || 
                analysisConfiguration.DatasetType == null ||
                analysisConfiguration.DatasetType.NMI == null ||
                analysisConfiguration.DatasetType.Nrep == null
                )
            {
                return false;
            }

            if (!LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName))
            {
                return false;
            }

            if (subsettingExpression != null && !ApplySubsetting(subsettingExpression))
            {
                return false;
            }

            if (analysisConfiguration.ModeKeep == false && !ReduceToNecessaryVariables(analysisConfiguration.GetRegexNecessaryVariables()))
            {
                return false;
            }

            var repWgtsRegex = analysisConfiguration.DatasetType.RepWgts;
            if (analysisConfiguration.DatasetType.JKzone != null && analysisConfiguration.DatasetType.JKzone.Length != 0)
            {
                if (analysisConfiguration.DatasetType.Weight.Length == 0 || 
                    analysisConfiguration.DatasetType.JKrep == null || 
                    analysisConfiguration.DatasetType.JKrep.Length == 0)
                {
                    return false;
                }

                if (!CreateReplicateWeights((int)analysisConfiguration.DatasetType.Nrep, analysisConfiguration.DatasetType.Weight, analysisConfiguration.DatasetType.JKzone, analysisConfiguration.DatasetType.JKrep, (bool)analysisConfiguration.DatasetType.JKreverse))
                {
                    return false;
                }

                repWgtsRegex = "lsanalyzer_repwgt_";
            }

            if (!CreateBIFIEdataObject(analysisConfiguration.DatasetType.Weight, (int)analysisConfiguration.DatasetType.NMI, analysisConfiguration.DatasetType.MIvar, analysisConfiguration.DatasetType.PVvars, (int)analysisConfiguration.DatasetType.Nrep, repWgtsRegex, analysisConfiguration.DatasetType.FayFac))
            {
                return false;
            }

            return true;
        }

        public bool PrepareForAnalysis(Analysis analysis, List<string>? additionalVariables = null)
        {
            if (analysis.AnalysisConfiguration.DatasetType == null || !ReduceToNecessaryVariables(analysis, additionalVariables, analysis.SubsettingExpression))
            {
                return false;
            }

            var repWgtsRegex = analysis.AnalysisConfiguration.DatasetType.RepWgts;
            if (analysis.AnalysisConfiguration.DatasetType.JKzone != null && analysis.AnalysisConfiguration.DatasetType.JKzone.Length != 0)
            {
                if (analysis.AnalysisConfiguration.DatasetType.Weight.Length == 0 ||
                    analysis.AnalysisConfiguration.DatasetType.JKrep == null ||
                    analysis.AnalysisConfiguration.DatasetType.JKrep.Length == 0)
                {
                    return false;
                }

                if (!CreateReplicateWeights((int)analysis.AnalysisConfiguration.DatasetType.Nrep, analysis.AnalysisConfiguration.DatasetType.Weight, analysis.AnalysisConfiguration.DatasetType.JKzone, analysis.AnalysisConfiguration.DatasetType.JKrep, (bool)analysis.AnalysisConfiguration.DatasetType.JKreverse))
                {
                    return false;
                }

                repWgtsRegex = "lsanalyzer_repwgt_";
            }

            if (!CreateBIFIEdataObject(analysis.AnalysisConfiguration.DatasetType.Weight, (int)analysis.AnalysisConfiguration.DatasetType.NMI, analysis.AnalysisConfiguration.DatasetType.MIvar, analysis.AnalysisConfiguration.DatasetType.PVvars, (int)analysis.AnalysisConfiguration.DatasetType.Nrep, repWgtsRegex, analysis.AnalysisConfiguration.DatasetType.FayFac))
            {
                return false;
            }

            return true;
        }

        public virtual List<Variable>? GetCurrentDatasetVariables(AnalysisConfiguration analysisConfiguration)
        {
            try
            {
                DataFrame? variables = null;
                if (analysisConfiguration.ModeKeep == true)
                {
                    variables = _engine!.Evaluate("lsanalyzer_dat_BO$variables").AsDataFrame();
                } else
                {
                    variables = _engine!.Evaluate("data.frame(variable = colnames(lsanalyzer_dat_raw_stored))").AsDataFrame();
                }

                List<Variable> variableList = new();
                foreach (var variable in variables.GetRows())
                {
                    variableList.Add(new(variable.RowIndex, (string)variable["variable"], analysisConfiguration.HasSystemVariable((string)variable["variable"])));
                }

                if (analysisConfiguration.ModeKeep == false)
                {
                    var maxPosition = variableList.Last().Position + 1;
                    
                    if (!string.IsNullOrWhiteSpace(analysisConfiguration.DatasetType?.PVvars))
                    { 
                        var pvVars = analysisConfiguration.DatasetType.PVvars.Split(";");
                        foreach (var pvVar in pvVars)
                        {
                            variableList.RemoveAll(var => Regex.IsMatch(var.Name, pvVar));
                            variableList.Add(new(maxPosition++, pvVar, false));
                        }
                    }
                    
                    variableList.Add(new(maxPosition++, "one", false));
                }

                return variableList;
            }
            catch
            {
                return null;
            }
        }
        
        public List<GenericVector>? CalculateUnivar(AnalysisUnivar analysis)
        {
            try
            {
                if (analysis.Vars.Count == 0 ||
                    analysis.AnalysisConfiguration.ModeKeep == false && !PrepareForAnalysis(analysis))
                {
                    return null;
                }

                List<GenericVector> resultList = new();

                string baseCall = "lsanalyzer_result_univar <- BIFIEsurvey::BIFIE.univar(BIFIEobj = lsanalyzer_dat_BO, vars = c(" + string.Join(", ", analysis.Vars.ConvertAll(var => "'" + var.Name + "'")) + ")";
                string groupByArg = "";

                if (analysis.GroupBy.Count == 0)
                {
                    _engine!.Evaluate(baseCall + groupByArg + ")");
                    resultList.Add(_engine.GetSymbol("lsanalyzer_result_univar").AsList());
                } else if (analysis.GroupBy.Count > 0 && !analysis.CalculateOverall)
                {
                    groupByArg = ", group = c(" + string.Join(", ", analysis.GroupBy.ConvertAll(var => "'" + var.Name + "'")) + ")";
                    _engine!.Evaluate(baseCall + groupByArg + ")");
                    resultList.Add(_engine.GetSymbol("lsanalyzer_result_univar").AsList());
                } else
                {
                    var groupByCombinations = Combinations.GetCombinations(analysis.GroupBy);

                    for (int nGroups = 0; nGroups <= analysis.GroupBy.Count; nGroups++)
                    {
                        if (nGroups == 0)
                        {
                            groupByArg = "";
                            _engine!.Evaluate(baseCall + groupByArg + ")");
                            resultList.Add(_engine.GetSymbol("lsanalyzer_result_univar").AsList());
                        } else
                        {
                            var groupByCombinationsN = groupByCombinations.Where(combination => combination.Count == nGroups).ToList();
                            foreach (var combination in groupByCombinationsN)
                            {
                                groupByArg = ", group = c(" + string.Join(", ", combination.ConvertAll(var => "'" + var.Name + "'")) + ")";
                                _engine!.Evaluate(baseCall + groupByArg + ")");
                                resultList.Add(_engine.GetSymbol("lsanalyzer_result_univar").AsList());
                            }
                        }
                    }
                }

                return resultList;
            }
            catch
            {
                return null;
            }
        }

        public List<GenericVector>? CalculateMeanDiff(AnalysisMeanDiff analysis)
        {
            try
            {
                if (analysis.Vars.Count == 0 || analysis.GroupBy.Count == 0 ||
                    analysis.AnalysisConfiguration.ModeKeep == false && !PrepareForAnalysis(analysis))
                {
                        return null;
                }

                List<GenericVector> resultList = new();

                string baseCall = "lsanalyzer_result_univar <- BIFIEsurvey::BIFIE.univar(BIFIEobj = lsanalyzer_dat_BO, vars = c(" + string.Join(", ", analysis.Vars.ConvertAll(var => "'" + var.Name + "'")) + ")";

                if (!analysis.CalculateSeparately)
                {
                    string groupByArg = ", group = c(" + string.Join(", ", analysis.GroupBy.ConvertAll(var => "'" + var.Name + "'")) + ")";
                    _engine!.Evaluate(baseCall + groupByArg + ")");
                    _engine!.Evaluate("lsanalyzer_result_univar_test <- BIFIEsurvey::BIFIE.univar.test(lsanalyzer_result_univar)");
                    resultList.Add(_engine.GetSymbol("lsanalyzer_result_univar_test").AsList());
                } else
                {
                    foreach (var groupByVar in analysis.GroupBy)
                    {
                        string groupByArg = ", group = '" + groupByVar.Name + "'";
                        _engine!.Evaluate(baseCall + groupByArg + ")");
                        _engine!.Evaluate("lsanalyzer_result_univar_test <- BIFIEsurvey::BIFIE.univar.test(lsanalyzer_result_univar)");
                        resultList.Add(_engine.GetSymbol("lsanalyzer_result_univar_test").AsList());
                    }
                }

                return resultList;
            }
            catch
            {
                return null;
            }
        }

        public List<GenericVector>? CalculateFreq(AnalysisFreq analysis)
        {
            try
            {
                if (analysis.Vars.Count == 0 ||
                    analysis.AnalysisConfiguration.ModeKeep == false && !PrepareForAnalysis(analysis))
                {
                    return null;
                }

                List<GenericVector> resultList = new();

                string baseCall = "lsanalyzer_result_freq <- BIFIEsurvey::BIFIE.freq(BIFIEobj = lsanalyzer_dat_BO, vars = c(" + string.Join(", ", analysis.Vars.ConvertAll(var => "'" + var.Name + "'")) + ")";
                string groupByArg = "";

                if (analysis.GroupBy.Count == 0)
                {
                    _engine!.Evaluate(baseCall + groupByArg + ")");
                    resultList.Add(_engine.GetSymbol("lsanalyzer_result_freq").AsList());
                }
                else if (analysis.GroupBy.Count > 0 && !analysis.CalculateOverall)
                {
                    groupByArg = ", group = c(" + string.Join(", ", analysis.GroupBy.ConvertAll(var => "'" + var.Name + "'")) + ")";
                    _engine!.Evaluate(baseCall + groupByArg + ")");
                    resultList.Add(_engine.GetSymbol("lsanalyzer_result_freq").AsList());
                }
                else
                {
                    var groupByCombinations = Combinations.GetCombinations(analysis.GroupBy);

                    for (int nGroups = 0; nGroups <= analysis.GroupBy.Count; nGroups++)
                    {
                        if (nGroups == 0)
                        {
                            groupByArg = "";
                            _engine!.Evaluate(baseCall + groupByArg + ")");
                            resultList.Add(_engine.GetSymbol("lsanalyzer_result_freq").AsList());
                        }
                        else
                        {
                            var groupByCombinationsN = groupByCombinations.Where(combination => combination.Count == nGroups).ToList();
                            foreach (var combination in groupByCombinationsN)
                            {
                                groupByArg = ", group = c(" + string.Join(", ", combination.ConvertAll(var => "'" + var.Name + "'")) + ")";
                                _engine!.Evaluate(baseCall + groupByArg + ")");
                                resultList.Add(_engine.GetSymbol("lsanalyzer_result_freq").AsList());
                            }
                        }
                    }
                }

                return resultList;
            }
            catch
            {
                return null;
            }
        }

        public List<Variable>? GetDatasetVariables(string filename)
        {
            try
            {
                _engine!.Evaluate("lsanalyzer_some_file_raw <- foreign::read.spss('" + filename.Replace("\\", "/") + "', use.value.labels = FALSE, to.data.frame = TRUE, use.missings = TRUE)");
                var variables = _engine.Evaluate("colnames(lsanalyzer_some_file_raw)").AsCharacter();

                List<Variable> variableList = new();
                int vv = 0;
                foreach (var variable in variables)
                {
                    variableList.Add(new(++vv, variable, false));
                }

                return variableList;
            }
            catch
            {
                return null;
            }
        }

        public DataFrame? GetValueLabels(string variable)
        {
            try
            {
                _engine!.Evaluate("lsanalyzer_value_labels <- attr(lsanalyzer_dat_raw_stored[, '" + variable + "'], 'value.labels')");
                if (!_engine.GetSymbol("lsanalyzer_value_labels").IsVector())
                {
                    return null;
                }

                _engine.Evaluate("lsanalyzer_value_labels_df <- data.frame(value = lsanalyzer_value_labels, label = names(lsanalyzer_value_labels))");
                return _engine.GetSymbol("lsanalyzer_value_labels_df").AsDataFrame();
            } catch 
            { 
                return null; 
            }
        }
    }

    public class SubsettingInformation
    {
        public bool ValidSubset { get; set; }
        public bool MIvariance { get; set; } = false;
        public int NCases { get; set; }
        public int NSubset { get; set; }

        public string Stringify
        {
            get => ValidSubset ? "Subset has " + NSubset + " cases, data has " + NCases + " cases." : (MIvariance ? "Subsetting is not supported for variables with MI variance." : "Invalid subsetting expression.");
        }
    }
}
