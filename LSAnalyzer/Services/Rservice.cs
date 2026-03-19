using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using RDotNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Services.Stubs;

namespace LSAnalyzer.Services
{
    public class Rservice : IRservice
    {
        private readonly ILogging _logger;
        private REngine? _engine;
        private readonly string[] _rPackagesNecessary = [ "BIFIEsurvey", "foreign" ];

        private List<string> _lastVirtualVariableNames = [];
        
        public (string rHome, string rPath) RLocation { get; set; } = (string.Empty, string.Empty);
        
        [ExcludeFromCodeCoverage]
        public Rservice()
        {
            // parameter-less constructor for mocking only
            _logger = new LoggingStub();

            RLocation = new Configuration(string.Empty, null, new SettingsServiceStub(), new RegistryService()).GetRLocation() ?? (string.Empty, string.Empty);
        }

        public Rservice(ILogging logger) 
        {
            _logger = logger;
        }

        private SymbolicExpression EvaluateAndLog(string what, string? analysisName = null, bool oneLiner = false)
        {
            _logger.AddEntry(new LogEntry(DateTime.Now, what, analysisName, oneLiner));
            return _engine!.Evaluate(what);
        }

        public bool Connect()
        {
            if (string.IsNullOrWhiteSpace(RLocation.rHome) || string.IsNullOrWhiteSpace(RLocation.rPath))
            {
                return false;
            }

            try
            {
                REngine.SetEnvironmentVariables(rPath: RLocation.rPath, rHome: RLocation.rHome);
                _engine = REngine.GetInstance();
                _engine.ClearGlobalEnvironment();
                
                string[] a = _engine.Evaluate("paste0('Result: ', stats::sd(c(1,2,3)))").AsCharacter().ToArray();
                if (a.Length == 0 || a[0] != "Result: 1")
                {
                    return false;
                }

                EvaluateAndLog("options(BIFIEsurvey.quiet = TRUE)");
            } catch
            {
                Dispose();
                return false;
            }

            return true;
        }

        public bool IsConnected => _engine != null;

        public bool NecessaryPackagesConfirmed { get; private set; }

        [ExcludeFromCodeCoverage]
        public string? GetRVersion()
        {
            try
            {
                EvaluateAndLog("lsanalyzer_full_version_string <- paste(R.Version()$version.string, R.Version()$nickname, R.Version()$platform, sep = ' - ')");
                return _engine!.GetSymbol("lsanalyzer_full_version_string").AsCharacter().First();
            } catch
            {
                return null;
            }
        }

        public virtual bool CheckNecessaryRPackages(string? packageName = null)
        {
            string[] rPackagesToCheck = (packageName == null ? _rPackagesNecessary : new string[] { packageName });

            try
            {
                foreach (string rPackage in rPackagesToCheck)
                {
                    bool available = _engine?.Evaluate("nzchar(system.file(package='" + rPackage + "'))").AsLogical().First() ?? false;
                    if (!available)
                    {
                        return false;
                    }
                }

                NecessaryPackagesConfirmed = true;
                return true;
            } catch 
            {
                return false;
            }
        }

        public bool InstallNecessaryRPackages(string? packageName = null)
        {
            string[] rPackagesToInstall = (packageName == null ? _rPackagesNecessary : new string[] { packageName });

            try
            {
                bool userLibraryFolderConfigured = EvaluateAndLog("nzchar(Sys.getenv('R_LIBS_USER'))").AsLogical().First();
                if (!userLibraryFolderConfigured)
                {
                    return false;
                }

                EvaluateAndLog("if (!dir.exists(Sys.getenv('R_LIBS_USER'))) { dir.create(Sys.getenv('R_LIBS_USER'), recursive = TRUE) }");

                foreach (string rPackage in rPackagesToInstall)
                {
                    bool available = EvaluateAndLog("nzchar(system.file(package='" + rPackage + "'))").AsLogical().First();
                    if (!available)
                    {
                        try
                        {
                            EvaluateAndLog("utils::install.packages('" + rPackage + "', lib = Sys.getenv('R_LIBS_USER'), repos = 'https://cloud.r-project.org')");
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

        [ExcludeFromCodeCoverage]
        public string? GetBifieSurveyVersion()
        {
            try
            {
                EvaluateAndLog("lsanalyzer_bifiesurvey_version <- paste(utils::packageVersion('BIFIEsurvey'), sep = '.')");
                return _engine!.GetSymbol("lsanalyzer_bifiesurvey_version").AsCharacter().First();
            } catch
            {
                return null;
            }
        }

        [ExcludeFromCodeCoverage]
        public IRservice.UpdateResult UpdateBifieSurvey()
        {
            try
            {
                EvaluateAndLog("lsanalyzer_old_packages <- data.frame(utils::old.packages(repos = 'https://cloud.r-project.org'))");
                DataFrame? oldPackages = _engine.GetSymbol("lsanalyzer_old_packages").AsDataFrame();
                if (oldPackages == null || !oldPackages["Package"].AsCharacter().Contains("BIFIEsurvey"))
                {
                    return IRservice.UpdateResult.Unavailable;
                }

                EvaluateAndLog("utils::update.packages(repos = 'https://cloud.r-project.org', ask = FALSE, oldPkgs = 'BIFIEsurvey')");

                return IRservice.UpdateResult.Success;
            } catch
            {
                return IRservice.UpdateResult.Failure;
            }
        }

        public bool InjectAppFunctions()
        {
            try
            {
                EvaluateAndLog("""
                    lsanalyzer_func_quantile <- function(BIFIEobj, vars, breaks, useInterpolation = TRUE, mimicIdbAnalyzer = FALSE, group=NULL, group_values=NULL)
                    {
                      userfct <- function(X,w)
                      {
                        params <- numeric()
                        for (cc in 1:ncol(X))
                        {
                          vx <- X[,cc]
                          vw <- w
                          ord <- order(vx,na.last=TRUE)
                          vx <- vx[ord]
                          vw <- vw[ord]
                          if (any(is.na(vx)))
                          {
                            first_na <- min(which(is.na(vx)))
                            vx <- vx[1:(first_na-1)]
                            vw <- vw[1:(first_na-1)]
                          }
                          if (length(vx)>0)
                          {
                            relw <- cumsum(vw)/sum(vw)
                            agg <- data.frame(x=vx,w=relw)
                            for (bb in breaks)
                            {
                              if (any(agg$w<bb) && !all(agg$w<bb))
                              {
                                pos <- max(which(agg$w<bb))
                                lowx <- agg$x[pos]
                                loww <- agg$w[pos]
                                uppx <- agg$x[pos+1]
                                uppw <- agg$w[pos+1]
                                if (useInterpolation) param <- lowx + ((uppx-lowx) * (bb-loww) / (uppw - loww + 10^-20))
                                if (!useInterpolation && !mimicIdbAnalyzer) param <- lowx
                                if (!useInterpolation && mimicIdbAnalyzer) param <- uppx
                                params <- c(params,param)
                              } else
                              {
                                params <- c(params,NaN)
                              }
                            }
                          } else
                          {
                            params <- c(params,rep(NaN,length(breaks)))
                          }
                        }
                        return(params)
                      }

                      userparnames <- character()
                      for (vv in vars) userparnames <- c(userparnames,paste0(vv,"_yval_",breaks))
                      res <- BIFIEsurvey::BIFIE.by(BIFIEobj = BIFIEobj,
                                      vars = vars,
                                      userfct = userfct,
                                      userparnames = userparnames,
                                      group = group,
                                      group_values = group_values)

                      res$stat$var <- sub("\\_yval\\_([0-9]|\\.)*$", "", res$stat$parm)
                      res$stat$yval <- as.numeric(sub("^.*\\_yval\\_", "", res$stat$parm))
                      res$stat$quant <- res$stat$est

                      return(res)
                    }
                    """, null, true);

                EvaluateAndLog("""
                    lsanalyzer_func_factorScores <- function(x, na.rm)
                    {
                      rownames(x) <- 1:nrow(x)
                      colnames(x) <- paste0("V", 1:ncol(x))
                      f <- stats::as.formula(paste0("~",paste(colnames(x), collapse = "+")))
                      fact <- stats::factanal(f, factors = 1, data = x, scores = "regression")
                      scores <- fact$scores
                      scores.df <- data.frame(rows = as.numeric(rownames(scores)), scores = scores)
                      scores.df <- merge(scores.df, data.frame(rows = 1:nrow(x)), all.y = TRUE)
                      return(scores.df$Factor1)
                    }
                    """, null, true);
                
                return true;
            } catch
            {
                return false;
            }
        }

        public bool LoadFileIntoGlobalEnvironment(string fileName, string? fileType = null)
        {
            try
            {
                if (fileType  == null)
                {
                    fileType = fileName.Substring(fileName.LastIndexOf('.') + 1);
                }

                switch (fileType!.ToLower())
                {
                    case "sav":
                        EvaluateAndLog("lsanalyzer_dat_raw_stored <- foreign::read.spss('" + fileName.Replace("\\", "/") + "', use.value.labels = FALSE, to.data.frame = TRUE, use.missings = TRUE)");
                        break;
                    case "rds":
                        EvaluateAndLog("lsanalyzer_dat_raw_stored <- readRDS('" + fileName.Replace("\\", "/") + "')");
                        break;
                    case "csv":
                        EvaluateAndLog("lsanalyzer_dat_raw_stored <- utils::read.csv('" + fileName.Replace("\\", "/") + "')");
                        break;
                    case "csv2":
                        EvaluateAndLog("lsanalyzer_dat_raw_stored <- utils::read.csv2('" + fileName.Replace("\\", "/") + "')");
                        break;
                    case "xlsx":
                        EvaluateAndLog("lsanalyzer_dat_raw_stored <- openxlsx::read.xlsx('" + fileName.Replace("\\", "/") + "', sheet = 1)");
                        break;
                    default:
                        return false;
                }

                _lastVirtualVariableNames = [];
                    
                EvaluateAndLog("lsanalyzer_dat_raw <- lsanalyzer_dat_raw_stored");

                var rawData = _engine!.GetSymbol("lsanalyzer_dat_raw_stored").AsDataFrame();
                if (rawData == null)
                {
                    return false;
                }
            } catch (Exception)
            {
                return false;
            }

            return true;
        }

        public bool SortRawDataStored(string sortBy)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(sortBy))
                {
                    EvaluateAndLog("lsanalyzer_dat_raw_stored_attributes <- lapply(lsanalyzer_dat_raw_stored, attributes)");
                    EvaluateAndLog("lsanalyzer_dat_raw_stored <- lsanalyzer_dat_raw_stored[order(lsanalyzer_dat_raw_stored$`" + sortBy + "`), ]");
                    EvaluateAndLog("for (vv in colnames(lsanalyzer_dat_raw_stored)) attributes(lsanalyzer_dat_raw_stored[,vv]) <- lsanalyzer_dat_raw_stored_attributes[[vv]]");
                }
                EvaluateAndLog("lsanalyzer_dat_raw <- lsanalyzer_dat_raw_stored");
                return true;
            } catch
            {
                return false;
            }
        }

        public bool ReplaceCharacterVariables()
        {
            try
            {
                EvaluateAndLog("""
                    for (colname in colnames(lsanalyzer_dat_raw_stored)) {
                        if (is.character(lsanalyzer_dat_raw_stored[, colname]) &&
                            any(is.na(as.numeric(lsanalyzer_dat_raw_stored[, colname])) != is.na(lsanalyzer_dat_raw_stored[, colname]))) {
                            lsanalyzer_factor1 <- factor(lsanalyzer_dat_raw_stored[, colname])
                            lsanalyzer_labels1 <- 1:length(levels(lsanalyzer_factor1))
                            attr(lsanalyzer_labels1, 'names') <- levels(lsanalyzer_factor1)
                            lsanalyzer_dat_raw_stored[, colname] <- as.numeric(lsanalyzer_factor1)
                            attr(lsanalyzer_dat_raw_stored[, colname], 'value.labels') <- lsanalyzer_labels1
                        }
                    }
                """, null, true);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public virtual SubsettingInformation TestSubsetting(string subsettingExpression, string? MIvar = null)
        {
            try
            {
                EvaluateAndLog("lsanalyzer_dat_raw_stored_subset <- subset(lsanalyzer_dat_raw_stored, " + subsettingExpression + ")");

                int nCases;
                int nSubset;

                if (string.IsNullOrWhiteSpace(MIvar))
                {
                    EvaluateAndLog("lsanalyzer_dat_raw_stored_ncases <- nrow(lsanalyzer_dat_raw_stored)");
                    nCases = _engine.GetSymbol("lsanalyzer_dat_raw_stored_ncases").AsInteger().First();

                    EvaluateAndLog("lsanalyzer_dat_raw_stored_nsubset <- nrow(lsanalyzer_dat_raw_stored_subset)");
                    nSubset = _engine.GetSymbol("lsanalyzer_dat_raw_stored_nsubset").AsInteger().First();
                } else
                {
                    EvaluateAndLog("lsanalyzer_cnt_subset_mi <- table(lsanalyzer_dat_raw_stored_subset$" + MIvar + ")");
                    EvaluateAndLog("lsanalyzer_cnt_subset_mi_max <- max(lsanalyzer_cnt_subset_mi)");
                    EvaluateAndLog("lsanalyzer_cnt_subset_mi_all_equal <- all(lsanalyzer_cnt_subset_mi == lsanalyzer_cnt_subset_mi_max)");
                    var allMIEqual = _engine.GetSymbol("lsanalyzer_cnt_subset_mi_all_equal").AsLogical().First();
                    if (!allMIEqual)
                    {
                        return new SubsettingInformation() { ValidSubset = false, MIvariance = true };
                    }

                    EvaluateAndLog("lsanalyzer_dat_raw_stored_ncases_mi1 <- sum(!is.na(lsanalyzer_dat_raw_stored[, '" + MIvar + "']) & lsanalyzer_dat_raw_stored[, '" + MIvar + "'] == unique(lsanalyzer_dat_raw_stored[, '" + MIvar + "'])[1])");
                    nCases = _engine.GetSymbol("lsanalyzer_dat_raw_stored_ncases_mi1").AsInteger().First();

                    EvaluateAndLog("lsanalyzer_dat_raw_stored_nsubset_mi1 <- sum(!is.na(lsanalyzer_dat_raw_stored_subset[, '" + MIvar + "']) & lsanalyzer_dat_raw_stored_subset[, '" + MIvar + "'] == unique(lsanalyzer_dat_raw_stored_subset[, '" + MIvar + "'])[1])");
                    nSubset = _engine.GetSymbol("lsanalyzer_dat_raw_stored_nsubset_mi1").AsInteger().First();
                }

                EvaluateAndLog("rm(lsanalyzer_dat_raw_stored_subset)");

                if (nSubset == 0)
                {
                    return new SubsettingInformation() { ValidSubset = false, EmptySubset = true };
                }

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
                EvaluateAndLog("lsanalyzer_dat_raw <- subset(lsanalyzer_dat_raw, " + subsettingExpression + ")");
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
                EvaluateAndLog("lsanalyzer_dat_raw <- lsanalyzer_dat_raw_stored");

                EvaluateAndLog("lsanalyzer_necessary_variables <- numeric(0)");
                foreach (string regexNecessaryVariable in regexNecessaryVariables)
                {
                    EvaluateAndLog("lsanalyzer_necessary_variable <- grep('" + regexNecessaryVariable + "', colnames(lsanalyzer_dat_raw))");
                    if (_engine!.GetSymbol("lsanalyzer_necessary_variable").AsNumeric().Length == 0)
                    {
                        return false;
                    }
                    EvaluateAndLog("lsanalyzer_necessary_variables <- unique(c(lsanalyzer_necessary_variables, lsanalyzer_necessary_variable))");
                }

                if (!string.IsNullOrWhiteSpace(subsettingExpression) && !ApplySubsetting(subsettingExpression))
                {
                    return false;
                }

                EvaluateAndLog("lsanalyzer_dat_raw <- lsanalyzer_dat_raw[, lsanalyzer_necessary_variables]");
                var rawData = _engine.GetSymbol("lsanalyzer_dat_raw").AsDataFrame();
                if (rawData == null)
                {
                    return false;
                }
            }
            catch (Exception)
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

            for (int i = 0; i < regexNecessaryVariables.Count; i++)
            {
                var necessaryVariable = regexNecessaryVariables[i];
                var potentialPvVariable = analysis.AnalysisConfiguration.DatasetType!.PVvarsList.Where(pvvar => pvvar.DisplayName == necessaryVariable).FirstOrDefault();
                if (potentialPvVariable != null)
                {
                    regexNecessaryVariables[i] = potentialPvVariable.Regex;
                }
            }

            return ReduceToNecessaryVariables(regexNecessaryVariables, subsettingExpression);
        }

        public bool CreateReplicateWeights(string weight, string jkzone, string jkrep, bool jkreverse)
        {
            try
            {
                EvaluateAndLog("lsanalyzer_jk_zones <- sort(unique(lsanalyzer_dat_raw_stored[,'" + jkzone + "']))");
                EvaluateAndLog(
                    "for (lsanalyzer_jk_zone in lsanalyzer_jk_zones) " +
                    "   lsanalyzer_dat_raw[, paste0('lsanalyzer_repwgt_', lsanalyzer_jk_zone)] <- " +
                    "       lsanalyzer_dat_raw[,'" + weight + "'] * (lsanalyzer_dat_raw[, '" + jkzone + "'] != lsanalyzer_jk_zone) + " +
                    "       lsanalyzer_dat_raw[,'" + weight + "'] * (lsanalyzer_dat_raw[, '" + jkzone + "'] == lsanalyzer_jk_zone) * lsanalyzer_dat_raw[, '" + jkrep + "'] * 2;");
                if (jkreverse)
                {
                    EvaluateAndLog(
                        "for (lsanalyzer_jk_zone in lsanalyzer_jk_zones) " +
                        "   lsanalyzer_dat_raw[, paste0('lsanalyzer_repwgt_', lsanalyzer_jk_zone + max(lsanalyzer_jk_zones))] <- " +
                        "       lsanalyzer_dat_raw[,'" + weight + "'] * (lsanalyzer_dat_raw[, '" + jkzone + "'] != lsanalyzer_jk_zone) + " +
                        "       lsanalyzer_dat_raw[,'" + weight + "'] * (lsanalyzer_dat_raw[, '" + jkzone + "'] == lsanalyzer_jk_zone) * (1 - lsanalyzer_dat_raw[, '" + jkrep + "']) * 2;");
                }
                EvaluateAndLog("lsanalyzer_repwgts <- grep('lsanalyzer_repwgt_', colnames(lsanalyzer_dat_raw), value = TRUE);");
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public bool CreateBIFIEdataObject(string weight, int nmi, string? mivar, ICollection<PlausibleValueVariable>? pvvars, string? repwgts, double? fayfac, bool autoEncapsulatePVvars = false)
        {
            try
            {
                string rawDataVariableName = "lsanalyzer_dat_raw";
                if (mivar != null && mivar.Length > 0 && nmi > 1)
                {
                    EvaluateAndLog("lsanalyzer_dat_raw_list <- split(lsanalyzer_dat_raw, lsanalyzer_dat_raw[, '" + mivar + "'])");
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
                if (pvvars != null && pvvars.Count > 0)
                {
                    List<string> pvvarsList = new();

                    foreach (var pvvar in pvvars)
                    {
                        if (!pvvar.Mandatory)
                        {
                            var optionalPvvar = StringFormats.EncapsulateRegex(pvvar.Regex, autoEncapsulatePVvars)!;
                            var optionalPvExists = EvaluateAndLog("any(grepl('" + optionalPvvar + "', colnames(lsanalyzer_dat_raw)))").AsLogical().First();

                            if (optionalPvExists)
                            {
                                pvvarsList.Add(optionalPvvar);
                            }
                        } else
                        {
                            pvvarsList.Add(StringFormats.EncapsulateRegex(pvvar.Regex, autoEncapsulatePVvars)!);
                        }
                    }

                    if (pvvarsList.Count > 0)
                    {
                        var pvvarsArray = pvvarsList.ToArray();
                        pvvarsArray = Array.ConvertAll(pvvarsArray, pvvar => "'" + pvvar + "'");
                        pvvarsArg = ", pv_vars = c(" + String.Join(", ", pvvarsArray) + ")";
                    }
                }

                string finalCall = baseCall + repwgtArg + fayfacArg + pvvarsArg + ", cdata = TRUE)";
                EvaluateAndLog(finalCall);

                var bifieDataObject = _engine.GetSymbol("lsanalyzer_dat_BO").AsList();
                var nmiReported = (int)bifieDataObject["Nimp"].AsNumeric().First();
                var nrepReported = (int)bifieDataObject["RR"].AsNumeric().First();
                if (nmi != nmiReported)
                {
                    return false;
                }

                foreach (var pvvar in pvvars ?? new Collection<PlausibleValueVariable>())
                {
                    EvaluateAndLog($"lsanalyzer_dat_BO$varnames[lsanalyzer_dat_BO$varnames == '{ pvvar.Regex }'] <- '{ pvvar.DisplayName }'");
                    EvaluateAndLog($"lsanalyzer_dat_BO$variables[lsanalyzer_dat_BO$variables$variable == '{ pvvar.Regex }', c('variable', 'variable_orig')] <- '{pvvar.DisplayName}'");
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        public virtual bool TestAnalysisConfiguration(AnalysisConfiguration analysisConfiguration, List<VirtualVariable> virtualVariables, string? subsettingExpression = null)
        {
            if (_engine == null || 
                analysisConfiguration.FileName == null || 
                analysisConfiguration.DatasetType == null ||
                analysisConfiguration.DatasetType.NMI == null
                )
            {
                return false;
            }

            foreach (var lastVirtualVariableName in _lastVirtualVariableNames)
            {
                EvaluateAndLog($"lsanalyzer_dat_raw_stored$`{lastVirtualVariableName}` <- NULL");
                // because a potential label for lastVirtualVariableName does no harm, it will not be removed
            }
            _lastVirtualVariableNames.Clear();

            List<PlausibleValueVariable> plausibleValueVariables = [];
            
            List<VirtualVariable> failedVirtualVariables = [];
            foreach (var virtualVariable in virtualVariables)
            {
                if (!CreateVirtualVariable(virtualVariable, analysisConfiguration.DatasetType.PVvarsList.ToList()))
                {
                    failedVirtualVariables.Add(virtualVariable);
                }
                else
                {
                    if (virtualVariable.FromPlausibleValues)
                    {
                        plausibleValueVariables.Add(new PlausibleValueVariable { DisplayName = virtualVariable.Name, Regex = $"^{virtualVariable.Name}_[0-9]+$", Mandatory = false });
                    }
                }
            }

            if (failedVirtualVariables.Count > 0)
            {
                WeakReferenceMessenger.Default.Send(new VirtualVariableErrorMessage { FailedVirtualVariables = failedVirtualVariables });
            }

            EvaluateAndLog("lsanalyzer_dat_raw <- lsanalyzer_dat_raw_stored");

            if (!string.IsNullOrWhiteSpace(subsettingExpression) && !ApplySubsetting(subsettingExpression))
            {
                return false;
            }

            if (analysisConfiguration.ModeKeep == false && !ReduceToNecessaryVariables(analysisConfiguration.GetRegexNecessaryVariables()))
            {
                return false;
            }

            var repWgtsRegex = StringFormats.EncapsulateRegex(analysisConfiguration.DatasetType.RepWgts, analysisConfiguration.DatasetType.AutoEncapsulateRegex);
            if (analysisConfiguration.DatasetType.JKzone != null && analysisConfiguration.DatasetType.JKzone.Length != 0)
            {
                if (analysisConfiguration.DatasetType.Weight.Length == 0 || 
                    analysisConfiguration.DatasetType.JKrep == null || 
                    analysisConfiguration.DatasetType.JKrep.Length == 0)
                {
                    return false;
                }

                if (!CreateReplicateWeights(analysisConfiguration.DatasetType.Weight, analysisConfiguration.DatasetType.JKzone, analysisConfiguration.DatasetType.JKrep, (bool)analysisConfiguration.DatasetType.JKreverse))
                {
                    return false;
                }

                repWgtsRegex = "lsanalyzer_repwgt_";
            }
            
            plausibleValueVariables.AddRange(analysisConfiguration.DatasetType.PVvarsList);

            if (!CreateBIFIEdataObject(analysisConfiguration.DatasetType.Weight, (int)analysisConfiguration.DatasetType.NMI, analysisConfiguration.DatasetType.MIvar, plausibleValueVariables, repWgtsRegex, analysisConfiguration.DatasetType.FayFac, analysisConfiguration.DatasetType.AutoEncapsulateRegex))
            {
                return false;
            }

            return true;
        }

        public bool PrepareForAnalysis(Analysis analysis)
        {
            List<string>? additionalVariables = analysis is AnalysisRegression analysisRegression
                ? [analysisRegression.Dependent?.Name ?? "insufficient_analysis_definition_"]
                : null;
            
            if (analysis.AnalysisConfiguration.DatasetType == null || !ReduceToNecessaryVariables(analysis, additionalVariables, analysis.SubsettingExpression))
            {
                return false;
            }

            var repWgtsRegex = StringFormats.EncapsulateRegex(analysis.AnalysisConfiguration.DatasetType.RepWgts, analysis.AnalysisConfiguration.DatasetType.AutoEncapsulateRegex);
            if (analysis.AnalysisConfiguration.DatasetType.JKzone != null && analysis.AnalysisConfiguration.DatasetType.JKzone.Length != 0)
            {
                if (analysis.AnalysisConfiguration.DatasetType.Weight.Length == 0 ||
                    analysis.AnalysisConfiguration.DatasetType.JKrep == null ||
                    analysis.AnalysisConfiguration.DatasetType.JKrep.Length == 0)
                {
                    return false;
                }

                if (!CreateReplicateWeights(analysis.AnalysisConfiguration.DatasetType.Weight, analysis.AnalysisConfiguration.DatasetType.JKzone, analysis.AnalysisConfiguration.DatasetType.JKrep, (bool)analysis.AnalysisConfiguration.DatasetType.JKreverse))
                {
                    return false;
                }

                repWgtsRegex = "lsanalyzer_repwgt_";
            }
            
            List<PlausibleValueVariable> plausibleValueVariables = [];
            foreach (var virtualVariable in analysis.VirtualVariables.Where(vv => vv.FromPlausibleValues))
            {
                plausibleValueVariables.Add(new PlausibleValueVariable { DisplayName = virtualVariable.Name, Regex = virtualVariable.Name, Mandatory = false });
            }
            plausibleValueVariables.AddRange(analysis.AnalysisConfiguration.DatasetType.PVvarsList);

            if (!CreateBIFIEdataObject(analysis.AnalysisConfiguration.DatasetType.Weight, (int)analysis.AnalysisConfiguration.DatasetType.NMI, analysis.AnalysisConfiguration.DatasetType.MIvar, plausibleValueVariables, repWgtsRegex, analysis.AnalysisConfiguration.DatasetType.FayFac, analysis.AnalysisConfiguration.DatasetType.AutoEncapsulateRegex))
            {
                return false;
            }

            return true;
        }

        public virtual List<Variable>? GetCurrentDatasetVariables(AnalysisConfiguration analysisConfiguration, List<VirtualVariable> virtualVariables, bool fromStoredRaw = false)
        {
            if (analysisConfiguration.DatasetType == null)
            {
                return null;
            }

            try
            {
                DataFrame? variables = null;
                if (analysisConfiguration.ModeKeep == true && !fromStoredRaw)
                {
                    variables = EvaluateAndLog("lsanalyzer_dat_BO$variables").AsDataFrame();
                } else
                {
                    variables = EvaluateAndLog("data.frame(variable = colnames(lsanalyzer_dat_raw_stored))").AsDataFrame();
                }

                var variableLabelsExpression = EvaluateAndLog("attr(lsanalyzer_dat_raw_stored, 'variable.labels')");
                Dictionary<string, string> variableLabels = new();
                if (variableLabelsExpression != null && variableLabelsExpression.AsCharacter() != null)
                {
                    var variableLabelsVector = variableLabelsExpression.AsCharacter();
                    for (int ll = 0; ll < variableLabelsVector.Length; ll++)
                    {
                        variableLabels.Add(variableLabelsVector.Names[ll], variableLabelsVector[ll]);
                    }
                }

                List<Variable> variableList = new();
                foreach (var variable in variables.GetRows())
                {
                    Variable newVariable = new(variable.RowIndex, (string)variable["variable"])
                    {
                        IsSystemVariable = analysisConfiguration.HasSystemVariable((string)variable["variable"]),
                        FromPlausibleValues = analysisConfiguration.DatasetType.PVvarsList.Any(pvVar => pvVar.DisplayName == (string)variable["variable"]),
                        IsVirtual = virtualVariables.Select(vv => vv.Name).Contains((string)variable["variable"]),
                    };

                    if (variableLabels.Keys.Contains(newVariable.Name))
                    {
                        newVariable.Label = variableLabels[newVariable.Name];
                    } else
                    {
                        for (int ll = 0; ll < variableLabels.Count; ll++)
                        {
                            try
                            {
                                if (Regex.IsMatch(variableLabels.Keys.ToList()[ll], newVariable.Name))
                                {
                                    newVariable.Label = variableLabels[variableLabels.Keys.ToList()[ll]];
                                    break;
                                }
                            }
                            catch
                            {
                                break;
                            }
                        }
                    }

                    variableList.Add(newVariable);
                }

                if (analysisConfiguration.ModeKeep == false && !fromStoredRaw)
                {
                    var maxPosition = variableList.Last().Position + 1;

                    List<PlausibleValueVariable> plausibleValueVariables = [];

                    foreach (var virtualVariable in virtualVariables.Where(vv => vv.FromPlausibleValues))
                    {
                        plausibleValueVariables.Add(new PlausibleValueVariable { DisplayName = virtualVariable.Name, Regex = $"{virtualVariable.Name}_", Mandatory = false });
                    }
                    
                    plausibleValueVariables.AddRange(analysisConfiguration.DatasetType.PVvarsList);
                    
                    foreach (var pvVar in plausibleValueVariables)
                    {
                        var pvVarRegex = StringFormats.EncapsulateRegex(pvVar.Regex, analysisConfiguration.DatasetType.AutoEncapsulateRegex)!;

                        var firstMatch = variableList.Where(var => Regex.IsMatch(var.Name, pvVarRegex)).FirstOrDefault();

                        if (firstMatch != null)
                        {
                            variableList.RemoveAll(var => Regex.IsMatch(var.Name, pvVarRegex));
                            Variable newVariable = new(maxPosition++, pvVar.DisplayName);
                            newVariable.FromPlausibleValues = true;
                            newVariable.Label = firstMatch?.Label;
                            variableList.Add(newVariable);
                        }
                    }
                                        
                    variableList.Add(new(maxPosition++, "one"));
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
                    EvaluateAndLog(baseCall + groupByArg + ")", analysis.AnalysisName);
                    resultList.Add(_engine.GetSymbol("lsanalyzer_result_univar").AsList());
                } else if (analysis.GroupBy.Count > 0 && !analysis.CalculateOverall)
                {
                    groupByArg = ", group = c(" + string.Join(", ", analysis.GroupBy.ConvertAll(var => "'" + var.Name + "'")) + ")";
                    EvaluateAndLog(baseCall + groupByArg + ")", analysis.AnalysisName);
                    EvaluateAndLog("lsanalyzer_result_univar$stat$lsanalyzer_rank <- unlist(lapply(split(lsanalyzer_result_univar$stat$M, factor(lsanalyzer_result_univar$stat$var, levels = unique(lsanalyzer_result_univar$stat$var))), rank, ties.method = 'min'))");
                    resultList.Add(_engine.GetSymbol("lsanalyzer_result_univar").AsList());
                } else
                {
                    var groupByCombinations = Combinations.GetCombinations(analysis.GroupBy);

                    for (int nGroups = 0; nGroups <= analysis.GroupBy.Count; nGroups++)
                    {
                        if (nGroups > 0 && nGroups < analysis.GroupBy.Count && !analysis.CalculateCrosswise) continue;
                        
                        if (nGroups == 0)
                        {
                            groupByArg = "";
                            EvaluateAndLog(baseCall + groupByArg + ")", analysis.AnalysisName);
                            resultList.Add(_engine.GetSymbol("lsanalyzer_result_univar").AsList());
                        } else
                        {
                            var groupByCombinationsN = groupByCombinations.Where(combination => combination.Count == nGroups).ToList();
                            foreach (var combination in groupByCombinationsN)
                            {
                                groupByArg = ", group = c(" + string.Join(", ", combination.ConvertAll(var => "'" + var.Name + "'")) + ")";
                                EvaluateAndLog(baseCall + groupByArg + ")", analysis.AnalysisName);
                                EvaluateAndLog("lsanalyzer_result_univar$stat$lsanalyzer_rank <- unlist(lapply(split(lsanalyzer_result_univar$stat$M, factor(lsanalyzer_result_univar$stat$var, levels = unique(lsanalyzer_result_univar$stat$var))), rank, ties.method = 'min'))");
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
                    EvaluateAndLog(baseCall + groupByArg + ")", analysis.AnalysisName);
                    EvaluateAndLog("lsanalyzer_result_univar_test <- BIFIEsurvey::BIFIE.univar.test(lsanalyzer_result_univar)", analysis.AnalysisName);
                    EvaluateAndLog("lsanalyzer_result_univar <- c(lsanalyzer_result_univar, lsanalyzer_result_univar_test)");
                    resultList.Add(_engine!.GetSymbol("lsanalyzer_result_univar").AsList());
                } else
                {
                    foreach (var groupByVar in analysis.GroupBy)
                    {
                        string groupByArg = ", group = '" + groupByVar.Name + "'";
                        EvaluateAndLog(baseCall + groupByArg + ")", analysis.AnalysisName);
                        EvaluateAndLog("lsanalyzer_result_univar_test <- BIFIEsurvey::BIFIE.univar.test(lsanalyzer_result_univar)", analysis.AnalysisName);
                        EvaluateAndLog("lsanalyzer_result_univar <- c(lsanalyzer_result_univar, lsanalyzer_result_univar_test)");
                        resultList.Add(_engine!.GetSymbol("lsanalyzer_result_univar").AsList());
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
                    EvaluateAndLog(baseCall + groupByArg + ")", analysis.AnalysisName);
                    resultList.Add(_engine.GetSymbol("lsanalyzer_result_freq").AsList());
                }
                else if (analysis.GroupBy.Count > 0 && !analysis.CalculateOverall)
                {
                    groupByArg = ", group = c(" + string.Join(", ", analysis.GroupBy.ConvertAll(var => "'" + var.Name + "'")) + ")";
                    EvaluateAndLog(baseCall + groupByArg + ")", analysis.AnalysisName);
                    EvaluateAndLog("lsanalyzer_result_freq$stat$lsanalyzer_rank <- as.numeric(NA)");
                    EvaluateAndLog("ff <- lsanalyzer_result_freq$stat$varval == min(lsanalyzer_result_freq$stat$varval)");
                    EvaluateAndLog("lsanalyzer_result_freq$stat[ff,]$lsanalyzer_rank <- unlist(lapply(split(lsanalyzer_result_freq$stat[ff,]$perc, factor(lsanalyzer_result_freq$stat[ff,]$var, levels = unique(lsanalyzer_result_freq$stat[ff,]$var))), rank, ties.method = 'min'))");
                    resultList.Add(_engine.GetSymbol("lsanalyzer_result_freq").AsList());
                }
                else
                {
                    var groupByCombinations = Combinations.GetCombinations(analysis.GroupBy);

                    for (int nGroups = 0; nGroups <= analysis.GroupBy.Count; nGroups++)
                    {
                        if (nGroups > 0 && nGroups < analysis.GroupBy.Count && !analysis.CalculateCrosswise) continue;
                        
                        if (nGroups == 0)
                        {
                            groupByArg = "";
                            EvaluateAndLog(baseCall + groupByArg + ")", analysis.AnalysisName);
                            resultList.Add(_engine.GetSymbol("lsanalyzer_result_freq").AsList());
                        }
                        else
                        {
                            var groupByCombinationsN = groupByCombinations.Where(combination => combination.Count == nGroups).ToList();
                            foreach (var combination in groupByCombinationsN)
                            {
                                groupByArg = ", group = c(" + string.Join(", ", combination.ConvertAll(var => "'" + var.Name + "'")) + ")";
                                EvaluateAndLog(baseCall + groupByArg + ")", analysis.AnalysisName);
                                EvaluateAndLog("lsanalyzer_result_freq$stat$lsanalyzer_rank <- as.numeric(NA)");
                                EvaluateAndLog("ff <- lsanalyzer_result_freq$stat$varval == min(lsanalyzer_result_freq$stat$varval)");
                                EvaluateAndLog("lsanalyzer_result_freq$stat[ff,]$lsanalyzer_rank <- unlist(lapply(split(lsanalyzer_result_freq$stat[ff,]$perc, factor(lsanalyzer_result_freq$stat[ff,]$var, levels = unique(lsanalyzer_result_freq$stat[ff,]$var))), rank, ties.method = 'min'))");
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

        public List<GenericVector>? CalculateBivariate(AnalysisFreq analysis)
        {
            try
            {
                if (analysis.Vars.Count == 0 || analysis.GroupBy.Count == 0 ||
                    analysis.AnalysisConfiguration.ModeKeep == false && !PrepareForAnalysis(analysis))
                {
                    return null;
                }

                List<GenericVector> resultList = new();

                string baseCall = "lsanalyzer_result_crosstab <- BIFIEsurvey::BIFIE.crosstab(BIFIEobj = lsanalyzer_dat_BO";
                
                foreach (var group in analysis.GroupBy)
                {
                    foreach (var variable in analysis.Vars)
                    {
                        string vars1arg = ", vars1 = '" + group.Name + "'";
                        string vars2arg = ", vars2 = '" + variable.Name + "'";
                        string finalCall = baseCall + vars1arg + vars2arg + ");";

                        EvaluateAndLog(finalCall, analysis.AnalysisName);
                        resultList.Add(_engine.GetSymbol("lsanalyzer_result_crosstab").AsList());
                    }
                }

                return resultList;
            }
            catch
            {
                return null;
            }
        }

        public List<GenericVector>? CalculatePercentiles(AnalysisPercentiles analysis)
        {
            try
            {
                if (analysis.Vars.Count == 0 || analysis.Percentiles.Count == 0 ||
                    analysis.AnalysisConfiguration.ModeKeep == false && !PrepareForAnalysis(analysis))
                {
                    return null;
                }

                List<GenericVector> resultList = new();

                string baseCall = string.Empty;
                string varsArg = ", vars = c(" + string.Join(", ", analysis.Vars.ConvertAll(var => "'" + var.Name + "'")) + ")";
                string breaksArg = ", breaks = c(" + string.Join(", ", analysis.Percentiles.OrderBy(val => val).Select(val => val.ToString(CultureInfo.InvariantCulture))) + ")";

                if (!analysis.CalculateSE)
                {
                    baseCall = "lsanalyzer_result_ecdf <- BIFIEsurvey::BIFIE.ecdf(BIFIEobj = lsanalyzer_dat_BO" + varsArg + breaksArg;
                    if (!analysis.UseInterpolation)
                    {
                        baseCall += ", quanttype = 2";
                    }
                } else
                {
                    baseCall = "lsanalyzer_result_ecdf <- lsanalyzer_func_quantile(BIFIEobj = lsanalyzer_dat_BO" + varsArg + breaksArg;
                    if (!analysis.UseInterpolation)
                    {
                        baseCall += ", useInterpolation = FALSE";
                    }
                    if (analysis.MimicIdbAnalyzer)
                    {
                        baseCall += ", mimicIdbAnalyzer = TRUE";
                    }
                }

                string groupByArg = "";

                if (analysis.GroupBy.Count == 0)
                {
                    EvaluateAndLog(baseCall + groupByArg + ")", analysis.AnalysisName);
                    resultList.Add(_engine.GetSymbol("lsanalyzer_result_ecdf").AsList());
                }
                else if (analysis.GroupBy.Count > 0 && !analysis.CalculateOverall)
                {
                    groupByArg = ", group = c(" + string.Join(", ", analysis.GroupBy.ConvertAll(var => "'" + var.Name + "'")) + ")";
                    EvaluateAndLog(baseCall + groupByArg + ")", analysis.AnalysisName);
                    resultList.Add(_engine.GetSymbol("lsanalyzer_result_ecdf").AsList());
                }
                else
                {
                    var groupByCombinations = Combinations.GetCombinations(analysis.GroupBy);

                    for (int nGroups = 0; nGroups <= analysis.GroupBy.Count; nGroups++)
                    {
                        if (nGroups > 0 && nGroups < analysis.GroupBy.Count && !analysis.CalculateCrosswise) continue;
                        
                        if (nGroups == 0)
                        {
                            groupByArg = "";
                            EvaluateAndLog(baseCall + groupByArg + ")", analysis.AnalysisName);
                            resultList.Add(_engine.GetSymbol("lsanalyzer_result_ecdf").AsList());
                        }
                        else
                        {
                            var groupByCombinationsN = groupByCombinations.Where(combination => combination.Count == nGroups).ToList();
                            foreach (var combination in groupByCombinationsN)
                            {
                                groupByArg = ", group = c(" + string.Join(", ", combination.ConvertAll(var => "'" + var.Name + "'")) + ")";
                                EvaluateAndLog(baseCall + groupByArg + ")", analysis.AnalysisName);
                                resultList.Add(_engine.GetSymbol("lsanalyzer_result_ecdf").AsList());
                            }
                        }
                    }
                }

                return resultList;
            } catch
            { 
                return null; 
            }
        }

        public List<GenericVector>? CalculateCorr(AnalysisCorr analysis)
        {
            try
            {
                if (analysis.Vars.Count == 0 ||
                    analysis.AnalysisConfiguration.ModeKeep == false && !PrepareForAnalysis(analysis))
                {
                    return null;
                }

                List<GenericVector> resultList = new();

                string baseCall = "lsanalyzer_result_corr <- BIFIEsurvey::BIFIE.correl(BIFIEobj = lsanalyzer_dat_BO, vars = c(" + string.Join(", ", analysis.Vars.ConvertAll(var => "'" + var.Name + "'")) + ")";
                string groupByArg = "";

                if (analysis.GroupBy.Count == 0)
                {
                    EvaluateAndLog(baseCall + groupByArg + ")", analysis.AnalysisName);
                    resultList.Add(_engine.GetSymbol("lsanalyzer_result_corr").AsList());
                }
                else if (analysis.GroupBy.Count > 0 && !analysis.CalculateOverall)
                {
                    groupByArg = ", group = c(" + string.Join(", ", analysis.GroupBy.ConvertAll(var => "'" + var.Name + "'")) + ")";
                    EvaluateAndLog(baseCall + groupByArg + ")", analysis.AnalysisName);
                    resultList.Add(_engine.GetSymbol("lsanalyzer_result_corr").AsList());
                }
                else
                {
                    var groupByCombinations = Combinations.GetCombinations(analysis.GroupBy);

                    for (int nGroups = 0; nGroups <= analysis.GroupBy.Count; nGroups++)
                    {
                        if (nGroups > 0 && nGroups < analysis.GroupBy.Count && !analysis.CalculateCrosswise) continue;
                        
                        if (nGroups == 0)
                        {
                            groupByArg = "";
                            EvaluateAndLog(baseCall + groupByArg + ")", analysis.AnalysisName);
                            resultList.Add(_engine.GetSymbol("lsanalyzer_result_corr").AsList());
                        }
                        else
                        {
                            var groupByCombinationsN = groupByCombinations.Where(combination => combination.Count == nGroups).ToList();
                            foreach (var combination in groupByCombinationsN)
                            {
                                groupByArg = ", group = c(" + string.Join(", ", combination.ConvertAll(var => "'" + var.Name + "'")) + ")";
                                EvaluateAndLog(baseCall + groupByArg + ")", analysis.AnalysisName);
                                resultList.Add(_engine.GetSymbol("lsanalyzer_result_corr").AsList());
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

        public List<GenericVector>? CalculateLinreg(AnalysisLinreg analysis)
        {
            return CalculateRegression("BIFIE.linreg", "R^2", analysis);
        }

        public List<GenericVector>? CalculateLogistReg(AnalysisLogistReg analysis)
        {
            return CalculateRegression("BIFIE.logistreg", "R2", analysis);
        }
        
        private List<GenericVector>? CalculateRegression(string method, string r2parameter, AnalysisRegression analysis)
        {
            if (analysis.Dependent == null || analysis.Vars.Count == 0 ||
                analysis.AnalysisConfiguration.ModeKeep == false && !PrepareForAnalysis(analysis))
            {
                return null;
            }

            if (analysis.Sequence == AnalysisRegression.RegressionSequence.AllIn || analysis.Vars.Count == 1)
            {
                var result = CalculateRegressionSingle(method, analysis.Dependent, analysis.Vars, analysis.WithIntercept, analysis.GroupBy, analysis.CalculateOverall, analysis.CalculateCrosswise);
                if (result == null)
                {
                    return null;
                }
                else
                {
                    return result;
                }
            }

            if (analysis.Sequence == AnalysisRegression.RegressionSequence.Forward)
            {
                List<Variable> usedPredictors = new();
                List<Variable> availablePredictors = new(analysis.Vars);
                List<GenericVector> resultList = new();

                while (availablePredictors.Count > 0)
                {
                    double maxR2 = double.MinValue;
                    Variable? bestNextPredictor = null;
                    GenericVector? bestNextModel = null;

                    foreach (var predictor in availablePredictors)
                    {
                        var result = CalculateRegressionSingle(method, analysis.Dependent, usedPredictors.Concat(new List<Variable>() { predictor }).ToList(), analysis.WithIntercept, new(), false, false);
                        if (result == null)
                        {
                            return null;
                        }

                        var stats = result[0]["stat"].AsDataFrame();
                        double R2 = (double)stats.GetRows().Where(row => (string)row["parameter"] == r2parameter).First()["est"];

                        if (R2 > maxR2)
                        {
                            maxR2 = R2;
                            bestNextPredictor = predictor;
                            bestNextModel = result[0];
                        }
                    }

                    usedPredictors.Add(bestNextPredictor!);
                    availablePredictors.Remove(bestNextPredictor!);
                    resultList.Add(bestNextModel!);
                }

                return resultList;
            }

            if (analysis.Sequence == AnalysisRegression.RegressionSequence.Backward)
            {
                var result = CalculateRegressionSingle(method, analysis.Dependent, analysis.Vars, analysis.WithIntercept, new(), false, false);
                if (result == null)
                {
                    return null;
                }

                List<Variable> usedPredictors = new(analysis.Vars);
                List<GenericVector> resultList = new() { result[0] };

                while (usedPredictors.Count > 1)
                {
                    double minR2 = double.MaxValue;
                    Variable? bestNextPredictor = null;
                    GenericVector? worstNextModel = null;

                    foreach (var predictor in usedPredictors)
                    {
                        result = CalculateRegressionSingle(method, analysis.Dependent, usedPredictors.Except(new List<Variable>() { predictor }).ToList(), analysis.WithIntercept, new(), false, false);
                        if (result == null)
                        {
                            return null;
                        }

                        var stats = result[0]["stat"].AsDataFrame();
                        double R2 = (double)stats.GetRows().Where(row => (string)row["parameter"] == r2parameter).First()["est"];

                        if (R2 < minR2)
                        {
                            minR2 = R2;
                            bestNextPredictor = predictor;
                            worstNextModel = result[0];
                        }
                    }

                    usedPredictors.Remove(bestNextPredictor!);
                    resultList.Add(worstNextModel!);
                }

                return resultList;
            }

            return null;
        }

        private List<GenericVector>? CalculateRegressionSingle(string method, Variable dependent, List<Variable> predictors, bool withIntercept, List<Variable> groups, bool calcualteOverall, bool calculateCrosswise)
        {
            try
            {
                List<GenericVector> resultList = new();

                string baseCall = "lsanalyzer_result_regression <- BIFIEsurvey::" + method + "(BIFIEobj = lsanalyzer_dat_BO, dep = '" + dependent.Name + "', pre = c(" + (withIntercept ? "'one', " : "") + string.Join(", ", predictors.ConvertAll(var => "'" + var.Name + "'")) + ")";
                string groupByArg = "";

                if (groups.Count == 0)
                {
                    EvaluateAndLog(baseCall + groupByArg + ")", method == "BIFIE.linreg" ? "Linear regression" : "Logistic regression");
                    resultList.Add(_engine.GetSymbol("lsanalyzer_result_regression").AsList());
                }
                else if (groups.Count > 0 && !calcualteOverall)
                {
                    groupByArg = ", group = c(" + string.Join(", ", groups.ConvertAll(var => "'" + var.Name + "'")) + ")";
                    EvaluateAndLog(baseCall + groupByArg + ")", method == "BIFIE.linreg" ? "Linear regression" : "Logistic regression");
                    resultList.Add(_engine.GetSymbol("lsanalyzer_result_regression").AsList());
                }
                else
                {
                    var groupByCombinations = Combinations.GetCombinations(groups);

                    for (int nGroups = 0; nGroups <= groups.Count; nGroups++)
                    {
                        if (nGroups > 0 && nGroups < groups.Count && !calculateCrosswise) continue;
                        
                        if (nGroups == 0)
                        {
                            groupByArg = "";
                            EvaluateAndLog(baseCall + groupByArg + ")", method == "BIFIE.linreg" ? "Linear regression" : "Logistic regression");
                            resultList.Add(_engine.GetSymbol("lsanalyzer_result_regression").AsList());
                        }
                        else
                        {
                            var groupByCombinationsN = groupByCombinations.Where(combination => combination.Count == nGroups).ToList();
                            foreach (var combination in groupByCombinationsN)
                            {
                                groupByArg = ", group = c(" + string.Join(", ", combination.ConvertAll(var => "'" + var.Name + "'")) + ")";
                                EvaluateAndLog(baseCall + groupByArg + ")", method == "BIFIE.linreg" ? "Linear regression" : "Logistic regression");
                                resultList.Add(_engine.GetSymbol("lsanalyzer_result_regression").AsList());
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
        
        public bool CreateVirtualVariable(VirtualVariable virtualVariable, List<PlausibleValueVariable>? pvVars = null, bool forPreview = false)
        {
            return virtualVariable switch
            {
                VirtualVariableCombine virtualVariableCombine => CreateVirtualVariableCombine(virtualVariableCombine, pvVars, forPreview),
                VirtualVariableScale virtualVariableScale => CreateVirtualVariableScale(virtualVariableScale, pvVars, forPreview),
                VirtualVariableRecode virtualVariableRecode => CreateVirtualVariableRecode(virtualVariableRecode, pvVars, forPreview),
                _ => throw new ArgumentOutOfRangeException(nameof(virtualVariable), virtualVariable, null)
            };
        }

        private bool CreateVirtualVariableCombine(VirtualVariableCombine virtualVariableCombine, List<PlausibleValueVariable>? pvVars, bool forPreview)
        {
            try
            {
                if (!virtualVariableCombine.FromPlausibleValues)
                {
                    return ComputeVirtualVariableCombine(virtualVariableCombine, forPreview);
                }

                if (pvVars is null) return false;
                
                Dictionary<string, List<string>> pvVarsNames = [];
                
                foreach (var pvVar in pvVars.Where(pvVar => virtualVariableCombine.Variables.Any(var => var.Name == pvVar.DisplayName)))
                {
                    pvVarsNames.Add(pvVar.DisplayName, _engine?.Evaluate($"""grep("{pvVar.Regex}", colnames(lsanalyzer_dat_raw_stored), value = TRUE)""").AsCharacter().Order().ToList() ?? []);
                }
                
                if (pvVarsNames.Count == 0) return false;

                var numberOfImputations = pvVarsNames.First().Value.Count;
                if (numberOfImputations == 0 || pvVarsNames.Any(entry => entry.Value.Count != numberOfImputations)) return false;

                for (var imputation = 0; imputation < numberOfImputations; imputation++)
                {
                    var virtualVariableClone = (virtualVariableCombine.Clone() as VirtualVariableCombine)!;
                    virtualVariableClone.Name = virtualVariableCombine.Name + "_" + (imputation + 1);
                    
                    foreach (var (name, varNames) in pvVarsNames)
                    {
                        virtualVariableClone.Variables.First(variable => variable.Name == name).Name = varNames[imputation];
                    }

                    if (!ComputeVirtualVariableCombine(virtualVariableClone, forPreview)) return false;
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool ComputeVirtualVariableCombine(VirtualVariableCombine virtualVariableCombine, bool forPreview)
        {
            try
            {
                if (virtualVariableCombine.Variables.Count == 0) return false;

                if (forPreview)
                {
                    var inputVariablesString = string.Join(", ", virtualVariableCombine.Variables.Select(v => "'" + v.Name + "'"));
                    EvaluateAndLog($"lsanalyzer_dat_raw_preview <- lsanalyzer_dat_raw_stored[,c({inputVariablesString}),drop=FALSE]");
                }

                var target = forPreview ? "lsanalyzer_dat_raw_preview" : "lsanalyzer_dat_raw_stored";
                
                var nameExists = _engine?.Evaluate($"'{virtualVariableCombine.Name}' %in% colnames({target})").AsLogical().First() ?? true;
                if (nameExists) return false;

                var assignment = $"{target}[,'{virtualVariableCombine.Name}']";
                
                var baseCall = virtualVariableCombine.Type switch
                {
                    VirtualVariableCombine.CombinationFunction.Sum => "rowSums",
                    VirtualVariableCombine.CombinationFunction.Mean => "rowMeans",
                    VirtualVariableCombine.CombinationFunction.FactorScores => "lsanalyzer_func_factorScores",
                    _ => throw new ArgumentOutOfRangeException(nameof(virtualVariableCombine), virtualVariableCombine.Type.ToString(), "not in enum")
                };

                var subset = $"subset({target}, select = c({string.Join(", ", virtualVariableCombine.Variables.ToList().ConvertAll(var => "'" + var.Name + "'"))}))";
                var removeNa = virtualVariableCombine.RemoveNa ? "TRUE" : "FALSE";
                
                var fullCall = $"{assignment} <- {baseCall}({subset}, na.rm = {removeNa})";
                
                EvaluateAndLog(fullCall);

                if (!string.IsNullOrWhiteSpace(virtualVariableCombine.Label) && _engine?.Evaluate($"'variable.labels' %in% names(attributes({target}))").AsLogical().First() is true)
                {
                    EvaluateAndLog($"attributes({target})$variable.labels['{virtualVariableCombine.Name}'] = '{virtualVariableCombine.Label}'");
                }
                
                _lastVirtualVariableNames.Add(virtualVariableCombine.Name);
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool CreateVirtualVariableScale(VirtualVariableScale virtualVariableScale, List<PlausibleValueVariable>? pvVars, bool forPreview)
        {
            try
            {
                if (virtualVariableScale.InputVariable is null) return false;
                
                if (!virtualVariableScale.FromPlausibleValues)
                {
                    return ComputeVirtualVariableScale(virtualVariableScale, forPreview);
                }

                var pvVar = pvVars?.FirstOrDefault(pvVar => pvVar.DisplayName == virtualVariableScale.InputVariable.Name);
                if (pvVar is null) return false;
                
                var baseVariableNames = _engine?.Evaluate($"""grep("{pvVar.Regex}", colnames(lsanalyzer_dat_raw_stored), value = TRUE)""").AsCharacter().Order().ToList();
                if (baseVariableNames is null || baseVariableNames.Count == 0) return false;
                
                for (var imputation = 0; imputation < baseVariableNames.Count; imputation++)
                {
                    var virtualVariableClone = (virtualVariableScale.Clone() as VirtualVariableScale)!;
                    virtualVariableClone.Name = virtualVariableScale.Name + "_" + (imputation + 1);
                    virtualVariableClone.InputVariable!.Name = baseVariableNames[imputation];
                    
                    if (!ComputeVirtualVariableScale(virtualVariableClone, forPreview)) return false;
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool ComputeVirtualVariableScale(VirtualVariableScale virtualVariableScale, bool forPreview)
        {
            try
            {
                if (virtualVariableScale.InputVariable is null || virtualVariableScale.WeightVariable is null) return false;

                if (forPreview)
                {
                    var addMiVariableToPreview = virtualVariableScale.MiVariable is null
                        ? string.Empty
                        : $", '{virtualVariableScale.MiVariable.Name}'";
                    EvaluateAndLog($"lsanalyzer_dat_raw_preview <- lsanalyzer_dat_raw_stored[,c('{virtualVariableScale.InputVariable.Name}', '{virtualVariableScale.WeightVariable.Name}'{addMiVariableToPreview}),drop=FALSE]");
                }

                var target = forPreview ? "lsanalyzer_dat_raw_preview" : "lsanalyzer_dat_raw_stored";
                
                var nameExists = _engine?.Evaluate($"'{virtualVariableScale.Name}' %in% colnames({target})").AsLogical().First() ?? true;
                if (nameExists) return false;

                switch (virtualVariableScale.Type)
                {
                    case VirtualVariableScale.ScaleType.Linear:
                        if (!ComputeVirtualVariableScaleLinear(virtualVariableScale, forPreview)) return false;
                        break;
                    case VirtualVariableScale.ScaleType.Logarithmic:
                        if (!ComputeVirtualVariableScaleLogarithmic(virtualVariableScale, forPreview)) return false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool ComputeVirtualVariableScaleLinear(VirtualVariableScale virtualVariableScale, bool forPreview)
        {
            // uses stats::weighted.mean with na.rm = TRUE to get a mean (per MI)
            // mimics Hmisc::wtd.var (5.2-4) with na.rm = TRUE to a get variance (per MI)
            try
            {
                if (virtualVariableScale.InputVariable is null || virtualVariableScale.WeightVariable is null) return false;
                
                var target = forPreview ? "lsanalyzer_dat_raw_preview" : "lsanalyzer_dat_raw_stored";
                var inputVariable = virtualVariableScale.InputVariable.Name;
                var weightVariable = virtualVariableScale.WeightVariable.Name;
                
                if (virtualVariableScale.MiVariable is null)
                {
                    var weightedMean = $"stats::weighted.mean({target}$`{inputVariable}`, w = {target}$`{weightVariable}`, na.rm = TRUE)";
                    EvaluateAndLog($"{target}$lsanalyzer_tmp_mean <- {weightedMean}");

                    var sumOfWeights = $"sum({target}$`{weightVariable}`[!is.na({target}$`{inputVariable}`)])";
                    var weightedSd = $"sqrt(sum({target}$`{weightVariable}` * ({target}$`{inputVariable}` - {target}$lsanalyzer_tmp_mean) ^ 2.0, na.rm = TRUE) / ({sumOfWeights} - 1))";
                    EvaluateAndLog($"{target}$lsanalyzer_tmp_sd <- {weightedSd}");
                }
                else
                {
                    var miVariable = virtualVariableScale.MiVariable.Name;
                    var weightedMean = $"stats::weighted.mean(imp1$`{inputVariable}`, w = imp1$`{weightVariable}`, na.rm = TRUE)";
                    var sumOfWeights = $"sum(imp1$`{weightVariable}`[!is.na(imp1$`{inputVariable}`)])";
                    var weightedSd = $"sqrt(sum(imp1$`{weightVariable}` * (imp1$`{inputVariable}` - {weightedMean}) ^ 2.0, na.rm = TRUE) / ({sumOfWeights} - 1))";
                    
                    EvaluateAndLog($$"""lsanalyzer_tmp_means <- do.call('rbind', lapply(split({{target}}, {{target}}$`{{miVariable}}`), FUN = function(imp1) { return(data.frame(mi = unique(imp1$`{{miVariable}}`), lsanalyzer_tmp_mean = {{weightedMean}}, lsanalyzer_tmp_sd = {{weightedSd}})) }))""");
                    EvaluateAndLog($"{target} <- merge({target}, lsanalyzer_tmp_means, by.x='{miVariable}', by.y='mi', all.x=TRUE)");
                }

                EvaluateAndLog($"{target}$`{virtualVariableScale.Name}` <- ({target}$`{inputVariable}` - {target}$lsanalyzer_tmp_mean) / {target}$lsanalyzer_tmp_sd * {virtualVariableScale.Sd.ToString(CultureInfo.InvariantCulture)} + {virtualVariableScale.Mean.ToString(CultureInfo.InvariantCulture)}");
                
                EvaluateAndLog($"{target}$lsanalyzer_tmp_mean <- NULL");
                EvaluateAndLog($"{target}$lsanalyzer_tmp_sd <- NULL");

                if (!string.IsNullOrWhiteSpace(virtualVariableScale.Label) && _engine?.Evaluate($"'variable.labels' %in% names(attributes({target}))").AsLogical().First() is true)
                {
                    EvaluateAndLog($"attributes({target})$variable.labels['{virtualVariableScale.Name}'] = '{virtualVariableScale.Label}'");
                }
                
                _lastVirtualVariableNames.Add(virtualVariableScale.Name);
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool ComputeVirtualVariableScaleLogarithmic(VirtualVariableScale virtualVariableScale, bool forPreview)
        {
            try
            {
                if (virtualVariableScale.InputVariable is null || virtualVariableScale.LogBase <= 1.0) return false;

                var target = forPreview ? "lsanalyzer_dat_raw_preview" : "lsanalyzer_dat_raw_stored";
                var inputVariable = virtualVariableScale.InputVariable.Name;
                
                if (!virtualVariableScale.Center)
                {
                    EvaluateAndLog($"{target}$`{virtualVariableScale.Name}` <- log({target}$`{inputVariable}`, base = {virtualVariableScale.LogBase.ToString(CultureInfo.InvariantCulture)})");
                }
                else
                {
                    if (virtualVariableScale.WeightVariable is null) return false;
                    
                    var weightVariable = virtualVariableScale.WeightVariable.Name;
                    
                    if (virtualVariableScale.MiVariable is null)
                    {
                        var weightedMean = $"stats::weighted.mean({target}$`{inputVariable}`, w = {target}$`{weightVariable}`, na.rm = TRUE)";
                        EvaluateAndLog($"{target}$lsanalyzer_tmp_mean <- {weightedMean}");
                    }
                    else
                    {
                        var miVariable = virtualVariableScale.MiVariable.Name;
                        var weightedMean = $"stats::weighted.mean(imp1$`{inputVariable}`, w = imp1$`{weightVariable}`, na.rm = TRUE)";
                        
                        EvaluateAndLog($$"""lsanalyzer_tmp_means <- do.call('rbind', lapply(split({{target}}, {{target}}$`{{miVariable}}`), FUN = function(imp1) { return(data.frame(mi = unique(imp1$`{{miVariable}}`), lsanalyzer_tmp_mean = {{weightedMean}})) }))""");
                        EvaluateAndLog($"{target} <- merge({target}, lsanalyzer_tmp_means, by.x='{miVariable}', by.y='mi', all.x=TRUE)");
                    }
                    
                    EvaluateAndLog($"{target}$`{virtualVariableScale.Name}` <- log({target}$`{inputVariable}` / {target}$lsanalyzer_tmp_mean, base = {virtualVariableScale.LogBase.ToString(CultureInfo.InvariantCulture)})");
                    
                    EvaluateAndLog($"{target}$lsanalyzer_tmp_mean <- NULL");
                }

                if (!string.IsNullOrWhiteSpace(virtualVariableScale.Label) && _engine?.Evaluate($"'variable.labels' %in% names(attributes({target}))").AsLogical().First() is true)
                {
                    EvaluateAndLog($"attributes({target})$variable.labels['{virtualVariableScale.Name}'] = '{virtualVariableScale.Label}'");
                }
                
                _lastVirtualVariableNames.Add(virtualVariableScale.Name);
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        private bool CreateVirtualVariableRecode(VirtualVariableRecode virtualVariableRecode, List<PlausibleValueVariable>? pvVars, bool forPreview)
        {
            try
            {
                if (!virtualVariableRecode.FromPlausibleValues)
                {
                    return ComputeVirtualVariableRecode(virtualVariableRecode, forPreview);
                }

                if (pvVars is null) return false;
                
                Dictionary<string, List<string>> pvVarsNames = [];
                
                foreach (var pvVar in pvVars.Where(pvVar => virtualVariableRecode.Variables.Any(var => var.Name == pvVar.DisplayName)))
                {
                    pvVarsNames.Add(pvVar.DisplayName, _engine?.Evaluate($"""grep("{pvVar.Regex}", colnames(lsanalyzer_dat_raw_stored), value = TRUE)""").AsCharacter().Order().ToList() ?? []);
                }
                
                if (pvVarsNames.Count == 0) return false;

                var numberOfImputations = pvVarsNames.First().Value.Count;
                if (numberOfImputations == 0 || pvVarsNames.Any(entry => entry.Value.Count != numberOfImputations)) return false;

                for (var imputation = 0; imputation < numberOfImputations; imputation++)
                {
                    var virtualVariableClone = (virtualVariableRecode.Clone() as VirtualVariableRecode)!;
                    virtualVariableClone.Name = virtualVariableRecode.Name + "_" + (imputation + 1);
                    
                    foreach (var (name, varNames) in pvVarsNames)
                    {
                        virtualVariableClone.Variables.First(variable => variable.Name == name).Name = varNames[imputation];
                    }

                    if (!ComputeVirtualVariableRecode(virtualVariableClone, forPreview)) return false;
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool ComputeVirtualVariableRecode(VirtualVariableRecode virtualVariableRecode, bool forPreview)
        {
            try
            {
                if (forPreview)
                {
                    if (virtualVariableRecode.Variables.Count > 0)
                    {
                        var inputVariablesString = string.Join(", ", virtualVariableRecode.Variables.Select(v => "'" + v.Name + "'"));
                        EvaluateAndLog($"lsanalyzer_dat_raw_preview <- lsanalyzer_dat_raw_stored[,c({inputVariablesString}),drop=FALSE]");
                    }
                    else
                    {
                        EvaluateAndLog($"lsanalyzer_dat_raw_preview <- data.frame(Input = numeric(nrow(lsanalyzer_dat_raw_stored)))");
                    }
                }
                
                var target = forPreview ? "lsanalyzer_dat_raw_preview" : "lsanalyzer_dat_raw_stored";
                
                var nameExists = _engine?.Evaluate($"'{virtualVariableRecode.Name}' %in% colnames({target})").AsLogical().First() ?? true;
                if (nameExists) return false;
                
                if (virtualVariableRecode is { Else: VirtualVariableRecode.ElseAction.Copy, Variables.Count: 0 }) return false;
                var elseResult = virtualVariableRecode.Else == VirtualVariableRecode.ElseAction.Missing ? "as.numeric(NA)" : $"{target}$`{virtualVariableRecode.Variables.First().Name}`";

                EvaluateAndLog($"{target}$`{virtualVariableRecode.Name}` <- {elseResult}");

                foreach (var rule in virtualVariableRecode.Rules)
                {
                    var criteria = rule.Criteria.Select(crit =>
                    {
                        var variable = $"{target}$`{virtualVariableRecode.Variables[crit.VariableIndex].Name}`";
                        var value = $"{crit.Value.ToString(CultureInfo.InvariantCulture)}";
                        var maxValue = $"{crit.MaxValue.ToString(CultureInfo.InvariantCulture)}";
                        
                        return crit.Type switch
                        {
                            VirtualVariableRecode.Term.TermType.Missing => $"is.na({variable})",
                            VirtualVariableRecode.Term.TermType.Exactly => $"{variable} == {value}",
                            VirtualVariableRecode.Term.TermType.Between => $"{variable} >= {value} & {variable} <= {maxValue}",
                            _ => throw new ArgumentOutOfRangeException()
                        };
                    }).ToList();

                    EvaluateAndLog($"lsanalyzer_tmp_filter <- {string.Join(" & ", criteria)}");
                    EvaluateAndLog("lsanalyzer_tmp_filter[is.na(lsanalyzer_tmp_filter)] <- FALSE");

                    var result = rule.ResultNa ? "NA" : rule.ResultValue.ToString(CultureInfo.InvariantCulture);
                    
                    EvaluateAndLog($"{target}$`{virtualVariableRecode.Name}`[lsanalyzer_tmp_filter] <- {result}");
                }

                if (!string.IsNullOrWhiteSpace(virtualVariableRecode.Label) && _engine?.Evaluate($"'variable.labels' %in% names(attributes({target}))").AsLogical().First() is true)
                {
                    EvaluateAndLog($"attributes({target})$variable.labels['{virtualVariableRecode.Name}'] = '{virtualVariableRecode.Label}'");
                }
                
                _lastVirtualVariableNames.Add(virtualVariableRecode.Name);
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        public (bool success, DataTable? dataTable) GetPreviewData()
        {
            try
            {
                _engine?.Evaluate("lsanalyzer_dat_raw_preview_distinct <- lsanalyzer_dat_raw_preview[!duplicated(lsanalyzer_dat_raw_preview),]");
                _engine?.Evaluate("if (nrow(lsanalyzer_dat_raw_preview_distinct) > 50) lsanalyzer_dat_raw_preview_distinct <- lsanalyzer_dat_raw_preview_distinct[1:50,]");
                _engine?.Evaluate("lsanalyzer_dat_raw_preview_distinct <- lsanalyzer_dat_raw_preview_distinct[do.call(order, lsanalyzer_dat_raw_preview_distinct),]");

                var previewData = Fetch("lsanalyzer_dat_raw_preview_distinct")?.AsDataFrame();
                
                return previewData is null ? (false, null) : (true, DataFrameToDataTable(previewData, "preview"));
            }
            catch
            {
                return (false, null);
            }
        }

        public List<Variable>? GetDatasetVariables(string fileName, string? fileType = null)
        {
            try
            {
                if (fileType == null)
                {
                    fileType = fileName.Substring(fileName.LastIndexOf('.') + 1);
                }

                switch (fileType!.ToLower())
                {
                    case "sav":
                        EvaluateAndLog("lsanalyzer_some_file_raw <- foreign::read.spss('" + fileName.Replace("\\", "/") + "', use.value.labels = FALSE, to.data.frame = TRUE, use.missings = TRUE)");
                        break;
                    case "rds":
                        EvaluateAndLog("lsanalyzer_some_file_raw <- readRDS('" + fileName.Replace("\\", "/") + "')");
                        break;
                    case "csv":
                        EvaluateAndLog("lsanalyzer_some_file_raw <- utils::read.csv('" + fileName.Replace("\\", "/") + "')");
                        break;
                    case "csv2":
                        EvaluateAndLog("lsanalyzer_some_file_raw <- utils::read.csv2('" + fileName.Replace("\\", "/") + "')");
                        break;
                    case "xlsx":
                        EvaluateAndLog("lsanalyzer_some_file_raw <- openxlsx::read.xlsx('" + fileName.Replace("\\", "/") + "', sheet = 1)");
                        break;
                    default:
                        return new();
                }

                var variables = EvaluateAndLog("colnames(lsanalyzer_some_file_raw)").AsCharacter();

                List<Variable> variableList = new();
                int vv = 0;
                foreach (var variable in variables)
                {
                    variableList.Add(new(++vv, variable));
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
                if (EvaluateAndLog("'" + variable + "' %in% colnames(lsanalyzer_dat_raw_stored)").AsLogical().First()) 
                {
                    EvaluateAndLog("lsanalyzer_value_labels <- attr(lsanalyzer_dat_raw_stored[, '" + variable + "'], 'value.labels')");
                } else
                {
                    // try to use variable name as regex but only return value labels if they are the same for all matches
                    EvaluateAndLog(@"# try to fetch value labels per variable name regex
                        lsanalyzer_value_labels <- NULL
                        lsanalyzer_possible_variables <- grep('" + variable + @"', colnames(lsanalyzer_dat_raw_stored), value = TRUE)
                        for (lsanalyzer_possible_variable in lsanalyzer_possible_variables) {
                            lsanalyzer_value_labels1 <- attr(lsanalyzer_dat_raw_stored[, lsanalyzer_possible_variable], 'value.labels')

                            if (!is.vector(lsanalyzer_value_labels1)) {
                                lsanalyzer_value_labels <- NULL
                                break
                            }

                            if (is.null(lsanalyzer_value_labels)) {
                                lsanalyzer_value_labels <- lsanalyzer_value_labels1
                            } else if (!identical(lsanalyzer_value_labels, lsanalyzer_value_labels1)) {
                                lsanalyzer_value_labels <- NULL
                                break
                            }
                        }
                    ", null, true);

                }

                if (!_engine!.GetSymbol("lsanalyzer_value_labels").IsVector())
                {
                    return null;
                }

                EvaluateAndLog("lsanalyzer_value_labels_df <- data.frame(value = lsanalyzer_value_labels, label = names(lsanalyzer_value_labels))");
                return _engine.GetSymbol("lsanalyzer_value_labels_df").AsDataFrame();
            } catch 
            { 
                return null; 
            }
        }

        public virtual bool Execute(string rCode, bool oneLiner = false)
        {
            try
            {
                EvaluateAndLog(rCode, null, oneLiner);
                return true;
            } catch
            {
                return false;
            }
        }

        public virtual SymbolicExpression? Fetch(string objectName)
        {
            try
            {
                return _engine!.GetSymbol(objectName);
            }
            catch
            {
                return null;
            }
        }

        private DataTable DataFrameToDataTable(DataFrame dataFrame, string name)
        {
            DataTable previewTable = new(name);
            for (var cc = 0; cc < dataFrame.ColumnCount; cc++)
            {
                previewTable.Columns.Add(dataFrame.ColumnNames[cc].Replace("_", "__"), typeof(double));
            }
            for (var rc = 0; rc < dataFrame.RowCount; rc++)
            {
                var newRow = previewTable.Rows.Add();
                for (var cc = 0; cc < dataFrame.ColumnCount; cc++)
                {
                    newRow[cc] = dataFrame[rc, cc];
                }
            }

            return previewTable;
        }

        public void SendUserInterrupt()
        {
            _engine?.UserInterrupt();
        }

        public void Dispose()
        {
            _engine?.Dispose();
            _engine = null;
        }

        public class VirtualVariableErrorMessage
        {
            public required List<VirtualVariable> FailedVirtualVariables { init; get; }
        }
    }

    public class SubsettingInformation
    {
        public bool ValidSubset { get; set; }
        public bool MIvariance { get; set; } = false;
        public bool EmptySubset { get; set; } = false;
        public int NCases { get; set; }
        public int NSubset { get; set; }

        public string Stringify
        {
            get => ValidSubset ? "Subset has " + NSubset + " cases, data has " + NCases + " cases." : 
                (MIvariance ? "Subsetting is not supported for variables with MI variance." : 
                (EmptySubset ? "Empty subset." : "Invalid subsetting expression."));
        }
    }
}
