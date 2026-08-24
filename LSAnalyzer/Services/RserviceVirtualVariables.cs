using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using LSAnalyzer.Models;
using RDotNet;

namespace LSAnalyzer.Services;

public partial class Rservice : VirtualVariableComputeBaseVisitor<string>, IRservice
{
    private string _currentTarget = "lsanalyzer_dat_raw_stored";
    private List<string> _tempVariableNames = [];
    private readonly Random _random = new Random();
    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    
    public bool CreateVirtualVariable(VirtualVariable virtualVariable, List<PlausibleValueVariable> pvVars, bool forPreview = false)
    {
        return virtualVariable switch
        {
            VirtualVariableCombine virtualVariableCombine => CreateVirtualVariableCombine(virtualVariableCombine, pvVars, forPreview),
            VirtualVariableScale virtualVariableScale => CreateVirtualVariableScale(virtualVariableScale, pvVars, forPreview),
            VirtualVariableRecode virtualVariableRecode => CreateVirtualVariableRecode(virtualVariableRecode, pvVars, forPreview),
            VirtualVariableCompute virtualVariableCompute => CreateVirtualVariableCompute(virtualVariableCompute, pvVars, forPreview),
            _ => throw new ArgumentOutOfRangeException(nameof(virtualVariable), virtualVariable, null)
        };
    }

    private bool CreateVirtualVariableCombine(VirtualVariableCombine virtualVariableCombine, List<PlausibleValueVariable> pvVars, bool forPreview)
    {
        try
        {
            if (!virtualVariableCombine.FromPlausibleValues)
            {
                return ComputeVirtualVariableCombine(virtualVariableCombine, forPreview);
            }
                
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

            EvaluateAndLog($"{assignment}[is.nan({assignment})] <- as.numeric(NA)");

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

    private bool CreateVirtualVariableScale(VirtualVariableScale virtualVariableScale, List<PlausibleValueVariable> pvVars, bool forPreview)
    {
        try
        {
            if (virtualVariableScale.InputVariable is null) return false;
                
            if (!virtualVariableScale.FromPlausibleValues)
            {
                return ComputeVirtualVariableScale(virtualVariableScale, forPreview);
            }

            var pvVar = pvVars.FirstOrDefault(pvVar => pvVar.DisplayName == virtualVariableScale.InputVariable.Name);
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
        
    private bool CreateVirtualVariableRecode(VirtualVariableRecode virtualVariableRecode, List<PlausibleValueVariable> pvVars, bool forPreview)
    {
        try
        {
            if (!virtualVariableRecode.FromPlausibleValues)
            {
                return ComputeVirtualVariableRecode(virtualVariableRecode, forPreview);
            }
                
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
            var elseResult = virtualVariableRecode.Else switch
            {
                VirtualVariableRecode.ElseAction.Missing => "as.numeric(NA)",
                VirtualVariableRecode.ElseAction.Copy => $"{target}$`{virtualVariableRecode.Variables.First().Name}`",
                VirtualVariableRecode.ElseAction.Set => $"{virtualVariableRecode.ElseValue.ToString(CultureInfo.InvariantCulture)}",
                _ => throw new ArgumentOutOfRangeException(),
            };

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
                        VirtualVariableRecode.Term.TermType.AtLeast => $"{variable} >= {value}",
                        VirtualVariableRecode.Term.TermType.AtMost => $"{variable} <= {maxValue}",
                        _ => throw new ArgumentOutOfRangeException()
                    };
                }).ToList();

                EvaluateAndLog($"lsanalyzer_tmp_filter <- {string.Join(" & ", criteria)}");
                EvaluateAndLog("lsanalyzer_tmp_filter[is.na(lsanalyzer_tmp_filter)] <- FALSE");

                var result = rule.ResultNa ? "NA" : rule.ResultValue.ToString(CultureInfo.InvariantCulture);
                    
                EvaluateAndLog($"{target}$`{virtualVariableRecode.Name}`[lsanalyzer_tmp_filter] <- {result}");
            }

            if (_engine?.Evaluate($"'variable.labels' %in% names(attributes({target}))").AsLogical().First() is true)
            {
                if (!string.IsNullOrWhiteSpace(virtualVariableRecode.Label))
                {
                    EvaluateAndLog($"attributes({target})$variable.labels['{virtualVariableRecode.Name}'] = '{virtualVariableRecode.Label}'");
                }

                var valueLabels = string.Join(", ", virtualVariableRecode.Rules.Where(r => !r.ResultNa && !string.IsNullOrWhiteSpace(r.Label)).Select(r => $"`{r.Label.Replace("`", "'")}` = {r.ResultValue.ToString(CultureInfo.InvariantCulture)}"));
                if (virtualVariableRecode.ElseValueMakesSense && !string.IsNullOrWhiteSpace(virtualVariableRecode.ElseLabel))
                {
                    var elseValueLabel = $"`{virtualVariableRecode.ElseLabel.Replace("`", "'")}` = {virtualVariableRecode.ElseValue.ToString(CultureInfo.InvariantCulture)}";
                    valueLabels = string.IsNullOrWhiteSpace(valueLabels) ? elseValueLabel : $"{valueLabels}, {elseValueLabel}";
                }

                if (!string.IsNullOrWhiteSpace(valueLabels))
                {
                    EvaluateAndLog($"attributes({target}$`{virtualVariableRecode.Name}`)$value.labels <- c({valueLabels})");
                }
            }

            _lastVirtualVariableNames.Add(virtualVariableRecode.Name);
                
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private bool CreateVirtualVariableCompute(VirtualVariableCompute virtualVariableCompute, List<PlausibleValueVariable> pvVars, bool forPreview)
    {
        try
        {
            virtualVariableCompute.PossiblePlausibleValueVariables = [..pvVars];
            
            if (!virtualVariableCompute.FromPlausibleValues)
            {
                return ComputeVirtualVariableCompute(virtualVariableCompute, forPreview);
            }
                
            Dictionary<string, List<string>> pvVarsNames = [];
            
            foreach (var pvVar in pvVars.Where(pvVar => virtualVariableCompute.Variables.Any(var => var == pvVar.DisplayName)))
            {
                pvVarsNames.Add(pvVar.DisplayName, _engine?.Evaluate($"""grep("{pvVar.Regex}", colnames(lsanalyzer_dat_raw_stored), value = TRUE)""").AsCharacter().Order().ToList() ?? []);
            }
            
            if (pvVarsNames.Count == 0) return false;

            var numberOfImputations = pvVarsNames.First().Value.Count;
            if (numberOfImputations == 0 || pvVarsNames.Any(entry => entry.Value.Count != numberOfImputations)) return false;

            for (var imputation = 0; imputation < numberOfImputations; imputation++)
            {
                var virtualVariableClone = (virtualVariableCompute.Clone() as VirtualVariableCompute)!;
                virtualVariableClone.Name = virtualVariableCompute.Name + "_" + (imputation + 1);
                    
                foreach (var (name, varNames) in pvVarsNames)
                {
                    VirtualVariableComputeLexer lexer = new(new AntlrInputStream(virtualVariableClone.Expression));
                    CommonTokenStream tokens = new(lexer);
                    VirtualVariableComputeParser parser = new(tokens);
                    ReplaceVariableNamesListener listener = new(tokens) { VariableName = name, Replacement = varNames[imputation] };
                    ParseTreeWalker.Default.Walk(listener, parser.expression());
        
                    virtualVariableClone.Expression = listener.GetReplacedExpression();
                }

                if (!ComputeVirtualVariableCompute(virtualVariableClone, forPreview)) return false;
            }
                
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private bool ComputeVirtualVariableCompute(VirtualVariableCompute virtualVariableCompute, bool forPreview)
    {
        try
        {
            if (!virtualVariableCompute.ValidExpression)
            {
                return false;
            }
            
            if (forPreview)
            {
                if (virtualVariableCompute.Variables.Count > 0)
                {
                    var inputVariablesString = string.Join(", ", virtualVariableCompute.Variables.Select(v => $"'{v}'"));
                    EvaluateAndLog($"lsanalyzer_dat_raw_preview <- lsanalyzer_dat_raw_stored[,c({inputVariablesString}),drop=FALSE]");
                }
                else
                {
                    EvaluateAndLog("lsanalyzer_dat_raw_preview <- data.frame(Input = numeric(nrow(lsanalyzer_dat_raw_stored)))");
                }
            }
                
            var target = forPreview ? "lsanalyzer_dat_raw_preview" : "lsanalyzer_dat_raw_stored";
                
            var nameExists = _engine?.Evaluate($"'{virtualVariableCompute.Name}' %in% colnames({target})").AsLogical().First() ?? true;
            if (nameExists) return false;

            _currentTarget = target;
            _tempVariableNames = [];
            var parser = virtualVariableCompute.GetParser();
            var lastTempVariableName = VisitExpression(parser.expression());
            
            EvaluateAndLog($"{target}$`{virtualVariableCompute.Name}` <- {target}$`{lastTempVariableName}`");
            foreach (var tempVariableName in _tempVariableNames)
            {
                EvaluateAndLog($"{target}$`{tempVariableName}` <- NULL");
            }
            
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

    public class VirtualVariableErrorMessage
    {
        public required List<VirtualVariable> FailedVirtualVariables { init; get; }
    }

    private string GetTempVariableName()
    {
        string tempVariableName;
        do
        {
            tempVariableName = $"lsanalyzer_tmp_{new string(Enumerable.Repeat(Chars, 8).Select(s => s[_random.Next(s.Length)]).ToArray())}";
        } while (_tempVariableNames.Contains(tempVariableName));
        _tempVariableNames.Add(tempVariableName);
        
        return tempVariableName;
    }

    public override string VisitVariable(VirtualVariableComputeParser.VariableContext context)
    {
        return context.GetText();
    }

    public override string VisitNumber(VirtualVariableComputeParser.NumberContext context)
    {
        var tempVariableName = GetTempVariableName();

        EvaluateAndLog($"{_currentTarget}$`{tempVariableName}` <- {context.GetText()}");
        
        return tempVariableName;
    }

    public override string VisitNegation(VirtualVariableComputeParser.NegationContext context)
    {
        var tempVariableName = GetTempVariableName();
        var childVariableName = Visit(context.value());

        switch (context.op.Text)
        {
            case "-":
                EvaluateAndLog($"{_currentTarget}$`{tempVariableName}` <- (0 - {_currentTarget}$`{childVariableName}`)");
                break;
            case "!":
                EvaluateAndLog($"{_currentTarget}$`{tempVariableName}` <- as.numeric(!as.logical({_currentTarget}$`{childVariableName}`))");
                break;
        }
        
        return tempVariableName;
    }

    public override string VisitExponent(VirtualVariableComputeParser.ExponentContext context)
    {
        var tempVariableName = GetTempVariableName();
        var leftChildVariableName = Visit(context.left);
        var rightChildVariableName = Visit(context.right);
        
        EvaluateAndLog($"{_currentTarget}$`{tempVariableName}` <- ({_currentTarget}$`{leftChildVariableName}` ^ {_currentTarget}$`{rightChildVariableName}`)");
        
        return tempVariableName;
    }

    public override string VisitOperation(VirtualVariableComputeParser.OperationContext context)
    {
        var tempVariableName = GetTempVariableName();
        var leftChildVariableName = Visit(context.left);
        var rightChildVariableName = Visit(context.right);
        
        EvaluateAndLog($"{_currentTarget}$`{tempVariableName}` <- ({_currentTarget}$`{leftChildVariableName}` {context.op.Text} {_currentTarget}$`{rightChildVariableName}`)");
        
        return tempVariableName;
    }

    public override string VisitIsNa(VirtualVariableComputeParser.IsNaContext context)
    {
        var tempVariableName = GetTempVariableName();
        var childVariableName = Visit(context.term());
        
        EvaluateAndLog($"{_currentTarget}$`{tempVariableName}` <- as.numeric(is.na({_currentTarget}$`{childVariableName}`))");
        
        return tempVariableName;
    }

    public override string VisitComparison(VirtualVariableComputeParser.ComparisonContext context)
    {
        var tempVariableName = GetTempVariableName();
        var leftChildVariableName = Visit(context.left);
        var rightChildVariableName = Visit(context.right);
        
        EvaluateAndLog($"{_currentTarget}$`{tempVariableName}` <- as.numeric({_currentTarget}$`{leftChildVariableName}` {context.op.Text} {_currentTarget}$`{rightChildVariableName}`)");
        
        return tempVariableName;
    }

    public override string VisitBoolean(VirtualVariableComputeParser.BooleanContext context)
    {
        var tempVariableName = GetTempVariableName();
        var leftChildVariableName = Visit(context.left);
        var rightChildVariableName = Visit(context.right);
        
        EvaluateAndLog($"{_currentTarget}$`{tempVariableName}` <- as.numeric(as.logical({_currentTarget}$`{leftChildVariableName}`) {context.op.Text} as.logical({_currentTarget}$`{rightChildVariableName}`))");
        
        return tempVariableName;
    }

    public override string VisitParentheses(VirtualVariableComputeParser.ParenthesesContext context)
    {
        return Visit(context.term());
    }

    public override string VisitExpression(VirtualVariableComputeParser.ExpressionContext context)
    {
        return Visit(context.term());
    }

    public class ReplaceVariableNamesListener(CommonTokenStream tokens) : VirtualVariableComputeBaseListener
    {
        private readonly TokenStreamRewriter _rewriter = new(tokens);
        
        public required string VariableName { get; init; }
        
        public required string Replacement { get; init; }

        public override void EnterVariable(VirtualVariableComputeParser.VariableContext context)
        {
            if (context.GetText() != VariableName)
            {
                return;
            }
            
            _rewriter.Replace(context.Start, context.Stop, Replacement);
        }

        public string GetReplacedExpression()
        {
            return _rewriter.GetText();
        }
    }
}