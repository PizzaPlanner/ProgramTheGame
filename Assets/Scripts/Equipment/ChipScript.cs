using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

public class CSToken
{
    public enum TokenType
    {
        Unknown,
        Register,
        Keyword,
        Parameter,
        Constant,
        Operator,
        LabelDeclaration,
        LabelAlias,
        Assignment,
        Commment,
    }
    
    public TokenType Type;
    public string Value;
}

public class CSLineToken
{
    public List<CSToken> Token;
    public TokenType LineType;
    public int Line;
    public enum TokenType
    {
        Empty,
        Invalid,
        Label,
        Assignment,
        Jump,
        JumpConditional
    }

    public void CalculateLineType()
    {
        var nonCommentToken = Token.Count(x => x.Type != CSToken.TokenType.Commment && x.Type != CSToken.TokenType.Unknown);
        if(nonCommentToken == 0)
            LineType = TokenType.Empty;
        else if(nonCommentToken == 1 && Token[0].Type == CSToken.TokenType.LabelDeclaration)
            LineType = TokenType.Label;
        else if(nonCommentToken >= 3 && Token[1].Type == CSToken.TokenType.Assignment)
            LineType = TokenType.Assignment;
        else if(nonCommentToken == 2 && Token[0].Value == "j")
            LineType = TokenType.Jump;
        else if(nonCommentToken == 3 && Token[0].Value == "jc")
            LineType = TokenType.JumpConditional;
        else LineType = TokenType.Invalid;
    }

    public string[] ToIntermediate()
    {
        if(LineType is TokenType.Invalid or TokenType.Empty)
            return Array.Empty<string>();
        if(LineType is TokenType.Label)
            return new []{ Token[0].Value };
        if(LineType is TokenType.Jump)
            return new []{ $"self.OpRhs = {Token[1].Value}", $"self.Operator = {(int)ChipOperation.Jump}" };
        if(LineType is TokenType.JumpConditional)
            return new []{ $"self.OpLhs = {Token[1].Value}", $"self.OpRhs = {Token[2].Value}", $"self.Operator = {(int)ChipOperation.JumpConditional}" };
        if (LineType is TokenType.Assignment)
        {
            return AssignmentRightSideToIntermediate().Concat(new[]{ $"{Token[0].Value} = self.OpResult" }).ToArray();
        }
        return Array.Empty<string>();
    }

    string[] AssignmentRightSideToIntermediate()
    {
        throw new NotImplementedException();
    }
}

public class ChipScriptParser
{
    public List<CSLineToken> Token;
    
    static readonly string[] Operators = new []{ ">=", ">", "<", "<=", "==", "!="};
    static readonly string[] Keywords = new [] { "not", "j", "jc" };

    public void Parse(ChipScript script)
    {
        Token = new List<CSLineToken>();
        var lines = script.Script.Split('\n');
        var i = 0;
        foreach(var line in lines)
            ParseLine(i++, line);
    }

    void ParseLine(int i, string line)
    {
        var lineToken = new CSLineToken(){ Token = new List<CSToken>(), Line = i };
        var isLeftOfAssignment = true;
        var isInComment = false;
        line = line.Trim() + '\n';
        var commentIdx = line.IndexOf('#');
        var equalityIdx = line.IndexOf('=');
        var wordStart = 0;
        var wordEnd = line.IndexOfAny(new[]{' ', '\t', '\n' }, wordStart);
        while (wordEnd > -1)
        {
            var word = line[wordStart..wordEnd];
            var token = new CSToken{Value = word};
            if(wordStart >= commentIdx && commentIdx != -1)
                token.Type = CSToken.TokenType.Commment;
            else if(word.EndsWith(':'))
                token.Type = CSToken.TokenType.LabelDeclaration;
            else if(word.StartsWith('$'))
                token.Type = CSToken.TokenType.Register;
            else if(word.StartsWith('@'))
                token.Type = CSToken.TokenType.LabelAlias;
            else if(word == "=")
                token.Type = CSToken.TokenType.Assignment;
            else if(Operators.Contains(word))
                token.Type = CSToken.TokenType.Operator;
            else if(Keywords.Contains(word))
                token.Type = CSToken.TokenType.Keyword;
            else if(word.Split('.', StringSplitOptions.RemoveEmptyEntries).Length == 2)
                token.Type = CSToken.TokenType.Parameter;
            else if(long.TryParse(word, out _))
                token.Type = CSToken.TokenType.Constant;
            else token.Type = CSToken.TokenType.Unknown;
            lineToken.Token.Add(token);
            wordStart = wordEnd + 1;
            wordEnd = line.IndexOfAny(new[]{' ', '\t', '\n' }, wordStart);
        }
        lineToken.CalculateLineType();
    }
}

public struct ChipScriptLine
{
    public bool IsValid;
    public bool WritesToExecutor;
    public bool ReadsFromExecutor;
    public Identifieable LeftId;
    public Identifieable RightId;
    public bool RightIsConstant;
    public int LeftIndex;
    public int RightIndex;
    
    public static ChipScriptLine From(string s, Identifieable selfId)
    {
        var lr = s.Split('=');
        if(lr.Length != 2)
            return default;
        
        var left = lr[0].Trim();
        var right = lr[1].Trim();
        
        var csl = new ChipScriptLine();
        if (left.StartsWith("self."))
        {
            csl.WritesToExecutor = true;
            csl.LeftId = selfId;
        }
        else
        {
            var idName = left.Split('.')[0];
            csl.LeftId = Identifieable.FromName($"id.core.{idName}");
            if (csl.LeftId == default)
            {
                Debug.LogError($"Invalid script line '{s}': No ID associated with {idName}");
                return csl;
            }
        }

        if (int.TryParse(right, out var constant))
        {
            csl.RightIndex = constant;
            csl.RightIsConstant = true;
        }
        else
        {
            var idName = right.Split('.')[0];
            if (idName == "self")
            {
                csl.ReadsFromExecutor = true;
                csl.RightId = selfId;
            }
            else
            {
                csl.RightId = Identifieable.FromName($"id.core.{idName}");
                if (csl.RightId == default)
                {
                    Debug.LogError($"Invalid script line '{s}': No ID associated with {idName}");
                    return csl;
                }
            }
            
            csl.RightIndex = -1;
            var pRight = ItemTypeLogicParametersMappingSystem.GetAllParameters(csl.RightId);
            for (int i = 0; i < pRight.Length; i++)
            {
                if(string.Equals(pRight[i].Name.ToString(), right.Split('.')[1], StringComparison.InvariantCultureIgnoreCase))
                    csl.RightIndex = i;
            }

            if (csl.RightIndex < 0)
            {
                Debug.LogError($"Invalid script line '{s}': ID {csl.RightId} has no Parameter {right.Split('.')[1]}");
                return csl;
            }
        }
        
        csl.LeftIndex = -1;
        var pLeft = ItemTypeLogicParametersMappingSystem.GetAllParameters(csl.LeftId);
        for (int i = 0; i < pLeft.Length; i++)
        {
            if(string.Equals(pLeft[i].Name.ToString(), left.Split('.')[1], StringComparison.InvariantCultureIgnoreCase))
                csl.LeftIndex = i;
        }
        if (csl.LeftIndex < 0)
        {
            Debug.LogError($"Invalid script line '{s}': ID {csl.LeftId} has no Parameter {left.Split('.')[1]}");
            return csl;
        }
        
        csl.IsValid = true;
        return csl;
    }
}

[CreateAssetMenu]
[Serializable]
public class ChipScript : ScriptableObject
{
    public string Location;
    public Guid UniqueId;
    [Multiline(40)]
    public string Script;

    public bool Compile(Entity e, ref SystemState state)
    {
        if(!ToIntermediate(out var lines))
            return false;
        
        var newInstructions = new NativeList<ChipInstructions>(Allocator.Temp);
        foreach (var line in lines)
        {
            var csl = ChipScriptLine.From(line, state.EntityManager.GetComponentData<Identifieable>(e));
            if(!csl.IsValid)
                return false;
            
            var inst = new ChipInstructions
            {
                RightIsConstant = csl.RightIsConstant,
                WritesToSelf = csl.WritesToExecutor
            };
            inst.Left = new VariableAccess{TypeId = csl.LeftId, TypeParameterIndex = csl.LeftIndex};
            inst.Right = new VariableAccess{TypeId = csl.RightId, TypeParameterIndex = csl.RightIndex};
            newInstructions.Add(inst);
        }
        
        var instructions = state.EntityManager.GetBuffer<ChipInstructions>(e);
        instructions.Clear();
        instructions.AddRange(newInstructions);
        
        return true;
    }

    public bool ToIntermediate(out string[] lines)
    {
        var labelLines = new Dictionary<string, int>();
        var list = Script.Split('\n').Select(x => x.Split('#')[0].Trim()).Where(x => x.Length > 0).ToList();
        for (var i = 0; i < list.Count; i++)
        {
            var line = list[i];
            if(ProcessLabel(line))
            {
                Debug.Log($"Line {i}: Labelled as {line}");
                labelLines.Add(line[..^1], i);
                list.RemoveAt(i--);
            }
        }
        
        var intermediate = string.Join('\n', list);
        foreach(var label in labelLines)
            intermediate = intermediate.Replace($"@{label.Key}", label.Value.ToString());

        lines = intermediate.Split('\n');
        Debug.Log($"Intermediate script:\n{intermediate}");
        return true;
    }

    bool ProcessLabel(string line)
    {
        return line.EndsWith(':');
    }
}