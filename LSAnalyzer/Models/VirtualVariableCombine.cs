using System.Collections.Generic;

namespace LSAnalyzer.Models;

public class VirtualVariableCombine : VirtualVariable
{
    public override string TypeName => "Combine";

    public enum CombinationFunction
    {
        Sum,
        Mean,
        FactorScores,
    }
    
    public CombinationFunction Type { get; set; } = CombinationFunction.Mean;
    
    public bool RemoveNa { get; set; } = true;

    public List<Variable> Variables { get; set; } = [];
    
    public override VirtualVariable Clone()
    {
        return new VirtualVariableCombine
        {
            Name = Name,
            Label = Label,
            ForFileName = ForFileName,
            ForDatasetTypeId = ForDatasetTypeId,
            FromPlausibleValues = FromPlausibleValues,
            Type = Type,
            RemoveNa = RemoveNa,
            Variables = new List<Variable>(Variables.ConvertAll(variable => variable.Clone())),
        };
    }
}