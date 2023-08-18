using LSAnalyzer.Models;
using Microsoft.Win32;
using RDotNet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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
            if (_engine == null) 
            { 
                return false;
            }

            foreach (string rPackage in _rPackages)
            {
                bool available = _engine.Evaluate("nzchar(system.file(package='" + rPackage + "'))").AsLogical().First();
                if (!available)
                {
                    return false;
                }
            }

            return true;
        }

        public bool InstallNecessaryRPackages()
        {
            if (_engine == null)
            {
                return false;
            }

            bool userLibraryFolderConfigured = _engine.Evaluate("nzchar(Sys.getenv('R_LIBS_USER'))").AsLogical().First();
            if (!userLibraryFolderConfigured) 
            {
                return false;
            }

            try
            {
                _engine.Evaluate("if (!dir.exists(Sys.getenv('R_LIBS_USER'))) { dir.create(Sys.getenv('R_LIBS_USER')) }");
            } catch
            {
                return false;
            }

            foreach (string rPackage in _rPackages)
            {
                bool available = _engine.Evaluate("nzchar(system.file(package='" + rPackage + "'))").AsLogical().First();
                if (!available)
                {
                    try
                    {
                        _engine.Evaluate("install.packages('" + rPackage + "', lib = Sys.getenv('R_LIBS_USER'), repos = 'https://cloud.r-project.org')");
                    } catch
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool LoadFileIntoGlobalEnvironment(string fileName)
        {
            if (_engine == null)
            {
                return false;
            }

            try
            {
                _engine.Evaluate("lsanalyzer_dat_raw <- foreign::read.spss('" + fileName.Replace("\\", "/") + "', use.value.labels = FALSE, to.data.frame = TRUE, use.missings = TRUE)");
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

        public bool CreateReplicateWeights(int nrep, string weight, string jkzone, string jkrep, bool jkreverse)
        {
            if (_engine == null)
            {
                return false;
            }

            try
            {
                _engine.Evaluate("lsanalyzer_jk_zones <- unique(lsanalyzer_dat_raw[,'" + jkzone + "'])");
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
            if (_engine == null)
            {
                return false;
            }

            try
            {
                string rawDataVariableName = "lsanalyzer_dat_raw";
                if (mivar != null && mivar.Length > 0)
                {
                    _engine.Evaluate("lsanalyzer_dat_raw_list <- split(lsanalyzer_dat_raw, lsanalyzer_dat_raw[, '" + mivar + "'])");
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
                _engine.Evaluate(finalCall);

                var bifieDataObject = _engine.GetSymbol("lsanalyzer_dat_BO").AsList();
                var nmiReported = (int)bifieDataObject["Nimp"].AsNumeric().First();
                var nrepReported = (int)bifieDataObject["RR"].AsNumeric().First();
                if (nmi != nmiReported || nrep != nrepReported)
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

        public bool TestAnalysisConfiguration(AnalysisConfiguration analysisConfiguration)
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

            var repWgtsRegex = analysisConfiguration.DatasetType.RepWgts;
            if (analysisConfiguration.DatasetType.JKzone != null && analysisConfiguration.DatasetType.JKzone.Length != 0)
            {
                if (analysisConfiguration.DatasetType.Weight.Length == 0 || 
                    analysisConfiguration.DatasetType.JKrep == null || analysisConfiguration.DatasetType.JKrep.Length == 0 ||
                    analysisConfiguration.DatasetType.JKreverse == null)
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

        public List<Variable>? GetCurrentDatasetVariables(AnalysisConfiguration analysisConfiguration)
        {
            if (_engine == null)
            {
                return null;
            }

            if (analysisConfiguration.ModeKeep == null || analysisConfiguration.ModeKeep == false)
            {
                throw new NotImplementedException("Analysis mode 'Build BIFIEdata object' has yet to be implemented!");
            }

            try
            {
                var variables = _engine.Evaluate("lsanalyzer_dat_BO$variables").AsDataFrame();

                List<Variable> variableList = new();
                foreach (var variable in variables.GetRows())
                {
                    variableList.Add(new(variable.RowIndex, (string)variable["variable"], analysisConfiguration.HasSystemVariable((string)variable["variable"])));
                }

                return variableList;
            }
            catch
            {
                return null;
            }
        }
        
        public GenericVector? CalculateUnivar(Analysis analysis)
        {
            if (_engine == null || analysis.Vars.Count == 0)
            {
                return null;
            }

            if (analysis.AnalysisConfiguration.ModeKeep == null || analysis.AnalysisConfiguration.ModeKeep == false)
            {
                throw new NotImplementedException("Analysis mode 'Build BIFIEdata object' has yet to be implemented!");
            }

            try
            {
                string baseCall = "lsanalyzer_result_univar <- BIFIEsurvey::BIFIE.univar(BIFIEobj = lsanalyzer_dat_BO, vars = c(" + string.Join(", ", analysis.Vars.ConvertAll(var => "'" + var.Name + "'")) + ")";

                string groupByArg = "";
                if (analysis.GroupBy.Count > 0)
                {
                    groupByArg = ", group = c(" + string.Join(", ", analysis.GroupBy.ConvertAll(var => "'" + var.Name + "'")) + ")";
                }

                string finalCall = baseCall + groupByArg + ")";

                _engine.Evaluate(finalCall);
                return _engine.GetSymbol("lsanalyzer_result_univar").AsList();
            }
            catch
            {
                return null;
            }
        }

        public List<Variable>? GetDatasetVariables(string filename)
        {
            if (_engine == null)
            {
                return null;
            }

            try
            {
                _engine.Evaluate("lsanalyzer_some_file_raw <- foreign::read.spss('" + filename.Replace("\\", "/") + "', use.value.labels = FALSE, to.data.frame = TRUE, use.missings = TRUE)");
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
    }
}
