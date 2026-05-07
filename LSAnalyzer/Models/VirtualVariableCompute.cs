using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CommunityToolkit.Mvvm.ComponentModel;
using LSAnalyzer.Helper;

namespace LSAnalyzer.Models;

public partial class VirtualVariableCompute : VirtualVariable
{
    [JsonIgnore]
    public List<PlausibleValueVariable> PossiblePlausibleValueVariables = [];
    
    public override string TypeName => "Compute";
    
    [ObservableProperty]
    private string _expression = string.Empty;
    partial void OnExpressionChanged(string value)
    {
        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(IsChanged));
    }

    public override bool FromPlausibleValues
    {
        get
        {
            if (!IsValid) return false;

            var expression = GetParser().expression();
            var rootNode = (expression.GetChild(0) as VirtualVariableComputeParser.TermContext)!;
            return HasPlausibleValueRecursive(rootNode);
        }
    }

    private bool HasPlausibleValueRecursive(VirtualVariableComputeParser.TermContext? term)
    {
        if (term is null) return false;
        
        if (term.children.Any(child => 
                child is ITerminalNode terminalNode && 
                terminalNode.Symbol.Type == VirtualVariableComputeLexer.VARIABLE && 
                PossiblePlausibleValueVariables.Any(pv => pv.DisplayName == terminalNode.GetText())))
        {
            return true;
        }

        return term.children.Aggregate(false, (current, termChild) => current || HasPlausibleValueRecursive(termChild as VirtualVariableComputeParser.TermContext));
    }

    public override string Info => Expression;

    [JsonIgnore]
    public bool IsValid
    {
        get
        {
            var expression = GetParser().expression();

            if (expression.children.Any(child => child is ErrorNodeImpl))
            {
                return false;
            }
            
            return !TermHasErrorRecursive(expression.GetChild(0) as VirtualVariableComputeParser.TermContext);
        }
    }
    
    public override VirtualVariable Clone()
    {
        return new VirtualVariableCompute
        {
            Name = Name,
            Label = Label,
            ForFileName = ForFileName,
            ForDatasetTypeId = ForDatasetTypeId,
            Expression = Expression,
        };
    }
    
    private VirtualVariableCompute? _savedState;

    public override void AcceptChanges()
    {
        _savedState = Clone() as VirtualVariableCompute;
        OnPropertyChanged(nameof(IsChanged));
    }

    [JsonIgnore]
    public override bool IsChanged 
    {
        get
        {
            OnPropertyChanged(nameof(Info));
            
            if (_savedState is null) return true;
            
            return !ObjectTools.PublicInstancePropertiesEqual(this, _savedState, [ "Info", "IsChanged", "Errors", "Variables" ]);
        }
    }
    
    private VirtualVariableComputeParser GetParser()
    {
        AntlrInputStream antlrInputStream = new(Expression);
        VirtualVariableComputeLexer lexer = new(antlrInputStream);
        CommonTokenStream commonTokenStream = new(lexer);
        VirtualVariableComputeParser parser = new(commonTokenStream);

        return parser;
    }
    
    public static bool TermHasErrorRecursive(VirtualVariableComputeParser.TermContext? term)
    {
        if (term is null) return false;
        
        return term.exception is not null || term.children.Any(t => TermHasErrorRecursive(t as VirtualVariableComputeParser.TermContext));
    }
}