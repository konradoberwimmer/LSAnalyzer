using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CommunityToolkit.Mvvm.ComponentModel;
using LSAnalyzer.Helper;
using LSAnalyzer.Models.ValidationAttributes;

namespace LSAnalyzer.Models;

public partial class VirtualVariableCompute : VirtualVariable, IAntlrErrorListener<IToken>
{
    private VariablesVisitor _variablesVisitor = new();
    
    [JsonIgnore]
    public List<PlausibleValueVariable> PossiblePlausibleValueVariables = [];
    
    public override string TypeName => "Compute";
    
    [ObservableProperty]
    [ValidComputeExpression("Not a valid expression.")]
    private string _expression = string.Empty;
    partial void OnExpressionChanged(string value)
    {
        _syntaxErrors = [];
        OnPropertyChanged(nameof(ValidExpression));
        OnPropertyChanged(nameof(IsChanged));
    }

    private List<SyntaxErrorEntry> _syntaxErrors = [];
    [JsonIgnore]
    public List<SyntaxErrorEntry> LastSyntaxErrors => _syntaxErrors;
    
    public override bool FromPlausibleValues => PossiblePlausibleValueVariables.Any(pv => Variables.Contains(pv.DisplayName));

    public override string Info => Expression;

    [JsonIgnore]
    public bool ValidExpression
    {
        get
        {
            _syntaxErrors = [];
            var parser = GetParser();
            parser.AddErrorListener(this);
            parser.expression();
            return _syntaxErrors.Count == 0;
        }
    }

    [JsonIgnore]
    public List<string> Variables
    {
        get
        {
            if (!ValidExpression) return [];
            
            var parser = GetParser();
            var variablesVisitor = new VariablesVisitor();
            return variablesVisitor.VisitExpression(parser.expression());
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
        _savedState!.Id = Id;
        OnPropertyChanged(nameof(IsChanged));
    }

    [JsonIgnore]
    public override bool IsChanged 
    {
        get
        {
            OnPropertyChanged(nameof(Info));
            
            if (_savedState is null) return true;
            
            return !ObjectTools.PublicInstancePropertiesEqual(this, _savedState, [ "Info", "IsChanged", "Errors", "ValidExpression", "LastSyntaxErrors", "Variables", "PossiblePlausibleValueVariables", "FromPlausibleValues" ]);
        }
    }

    public override bool InputVariableNamesExistIn(IEnumerable<Variable> variables)
    {
        throw new NotImplementedException();
    }

    public VirtualVariableComputeParser GetParser()
    {
        AntlrInputStream antlrInputStream = new(Expression);
        VirtualVariableComputeLexer lexer = new(antlrInputStream);
        lexer.RemoveErrorListener(ConsoleErrorListener<int>.Instance);
        CommonTokenStream commonTokenStream = new(lexer);
        VirtualVariableComputeParser parser = new(commonTokenStream);
        parser.RemoveErrorListener(ConsoleErrorListener<IToken>.Instance);

        return parser;
    }

    public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        _syntaxErrors.Add(new SyntaxErrorEntry(offendingSymbol, line, charPositionInLine, msg));
    }

    public record SyntaxErrorEntry(IToken? OffendingSymbol, int Line, int CharPositionInLine, string Message);

    private class VariablesVisitor : VirtualVariableComputeBaseVisitor<List<string>>
    {
        public override List<string> VisitVariable(VirtualVariableComputeParser.VariableContext context)
        {
            return [ context.GetText() ];
        }

        protected override List<string> DefaultResult => [];

        protected override List<string> AggregateResult(List<string> aggregate, List<string> nextResult)
        {
            aggregate.AddRange(nextResult);
            return aggregate.Distinct().ToList();
        }
    }
}