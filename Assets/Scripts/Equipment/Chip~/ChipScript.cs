using Identifiables;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

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
    
    public string Intermediate => Type switch
    {
        TokenType.Register => $"self.{Value[1..]}",
        TokenType.LabelDeclaration => Value[..^1],
        TokenType.Commment => "",
        _ => Value
    };
}

public class CSLineToken
{
    public List<CSToken> Token;
    public TokenType LineType;
    public int SourceLine;
    public int MappedToLine;
    public string OriginalLine;
    public string[] Intermediate;
    
    public ChipScriptLine[] ToScript(Identifiable executorId) => Intermediate.Select(x => ChipScriptLine.From(x, OriginalLine, executorId)).ToArray();
    
    public enum TokenType
    {
        Empty,
        Invalid,
        Label,
        SimpleAssignment,
        MathAssignment,
        LogicAssignment,
        Jump,
        JumpConditional
    }

    public void CalculateLineType()
    {
        var nonCommentToken = Token.Count(x => x.Type != CSToken.TokenType.Commment);
        if(Token.Any(x => x.Type == CSToken.TokenType.Unknown))
            LineType = TokenType.Invalid;
        else if(nonCommentToken == 0)
            LineType = TokenType.Empty;
        else if(nonCommentToken == 1 && Token[0].Type == CSToken.TokenType.LabelDeclaration)
            LineType = TokenType.Label;
        else if(nonCommentToken == 3 && Token[1].Value == "=")
            LineType = TokenType.SimpleAssignment;
        else if (nonCommentToken == 5 && Token[1].Value == "=" && Token[3].Type == CSToken.TokenType.Operator)
        {
            if(Token[3].Value is "+" or "-" or "*" or "/" or "%" )
                LineType = TokenType.MathAssignment;
            else
                LineType = TokenType.LogicAssignment;
        }
        else if(nonCommentToken == 2 && Token[0].Value == "j")
            LineType = TokenType.Jump;
        else if(nonCommentToken == 3 && Token[0].Value == "jc")
            LineType = TokenType.JumpConditional;
        
        Intermediate = ToIntermediate();
    }

    public string[] ToIntermediate()
    {
        if(LineType is TokenType.Invalid or TokenType.Empty or TokenType.Label)
            return Array.Empty<string>();
        if(LineType is TokenType.Jump)
            return new []{ $"self.OpRhs = {Token[1].Intermediate}", $"self.Operator = {(int)ChipOperation.Jump}" };
        if(LineType is TokenType.JumpConditional)
            return new []{ $"self.OpLhs = {Token[1].Intermediate}", $"self.OpRhs = {Token[2].Intermediate}", $"self.Operator = {(int)ChipOperation.JumpConditional}" };
        if (LineType is TokenType.SimpleAssignment)
            return new []{ $"{Token[0].Intermediate} = {Token[2].Intermediate}" };
        if (LineType is TokenType.MathAssignment or TokenType.LogicAssignment)
        {
            return AssignmentRightSideToIntermediate().Concat(new[]{ $"{Token[0].Intermediate} = self.OpResult" }).ToArray();
        }
        return Array.Empty<string>();
    }

    string[] AssignmentRightSideToIntermediate()
    {
        var left = Token[2];
        var right = Token[4];
        var op = Token[3].Value switch
        {
            ">=" => ChipOperation.GreaterEqual,
            ">" => ChipOperation.Greater,
            "<" => ChipOperation.Less,
            "<=" => ChipOperation.LessEqual,
            "==" => ChipOperation.Equal,
            "!=" => ChipOperation.NotEqual,
            "+" => ChipOperation.Add,
            "-" => ChipOperation.Sub,
            "*" => ChipOperation.Mul,
            "/" => ChipOperation.Div,
            "%" => ChipOperation.Mod,
        };
        return new []{ $"self.OpRhs = {right.Intermediate}", $"self.OpLhs = {left.Intermediate}", $"self.Operator = {(int)op}" };
    }

    public bool ReplaceLabels(Dictionary<string, CSLineToken> labelLines)
    {
        foreach(var token in Token.Where(x => x.Type == CSToken.TokenType.LabelAlias))
            for (int i = 0; i < Intermediate.Length; i++)
            {
                var label = token.Intermediate[1..];
                if (!labelLines.TryGetValue(label, out var labelledLine))
                {
                    Debug.LogError($"Error in line '{OriginalLine}': Label {label} not found");
                    return false;
                }
                Intermediate[i] = Intermediate[i].Replace(token.Value, labelledLine.MappedToLine.ToString());
            }
        return true;
    }
}

public class ChipScriptParser
{
    public List<CSLineToken> Token;
    
    static readonly string[] Operators = new []{ ">=", ">", "<", "<=", "==", "!=", "+", "-", "*", "/", "%"};
    static readonly string[] Keywords = new [] { "not", "j", "jc" };

    ChipScriptLine[] ResultFor(Identifiable executorId) => Token.SelectMany(x => x.ToScript(executorId)).ToArray();

    public Error LastError;
    
    public enum Error
    {
        NoError,
        KeyNotFound,
        Unknown,
        Empty,
        SyntaxError
    }
    public static Error GetFrom(LogicChip script, Identifiable executorId, out NativeArray<ChipInstructions> instructions)
    {
        instructions = default;
        if(script.LastError != Error.NoError)
            return script.LastError;
        var parser = new ChipScriptParser();
        parser.Parse(script);
        
        if(parser.LastError != Error.NoError)
            return parser.LastError;
        
        var newInstructions = new NativeList<ChipInstructions>(Allocator.Temp);
        foreach (var csl in parser.ResultFor(executorId))
        {
            if (!csl.IsValid)
                return Error.Unknown;

            var inst = new ChipInstructions
            {
                WritesToSelf = csl.WritesToExecutor
            };
            var rightConstant = csl.RightConstant;
            inst.Left = new ParameterAccess { TypeId = csl.LeftId, TypeParameterIndex = csl.LeftIndex };
            inst.Right = new AnyLogicValue()
            {
                LogicType = csl.RightType,
                Parameter = { TypeId = csl.RightId, TypeParameterIndex = csl.RightIndex },
                Constant = csl.RightConstant,
                IsConstant = csl.RightIsConstant
            };
            newInstructions.Add(inst);
        }
        instructions = newInstructions.ToArray(Allocator.Temp);
        Debug.Log($"Created {instructions.Length} compiled instructions for {script.Location}");
        if(instructions.Length == 0)
            return Error.Empty;
        return Error.NoError;
    }
    
    public void Parse(LogicChip script)
    {
        Token = new List<CSLineToken>();
        var lines = script.Script.Split('\n');
        var i = 0;
        foreach(var line in lines)
            ParseLine(i++, line);
        
        if(LastError != Error.NoError)
            return;
        
        if(!ResolveLabels())
            LastError = Error.KeyNotFound;
    }

    void ParseLine(int i, string line)
    {
        if(string.IsNullOrWhiteSpace(line))
            return;
        var lineToken = new CSLineToken(){ Token = new List<CSToken>(), SourceLine = i, OriginalLine = line.Trim() };
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
            else if(long.TryParse(word, out _) || Identifiable.FromName(word) != default)
                token.Type = CSToken.TokenType.Constant;
            else
                token.Type = CSToken.TokenType.Unknown;
            if(token.Value.Trim().Length > 0)
                lineToken.Token.Add(token);
            wordStart = wordEnd + 1;
            wordEnd = line.IndexOfAny(new[]{' ', '\t', '\n' }, wordStart);
        }
        lineToken.CalculateLineType();
        if (lineToken.LineType == CSLineToken.TokenType.Invalid)
        {
            LastError = Error.SyntaxError;
            Debug.Log($"Error in line {lineToken.OriginalLine}: Unexpected token '{lineToken.Token.First(x => x.Type == CSToken.TokenType.Unknown).Value}'");
            return;
        }
        Token.Add(lineToken);
    }
    
    public bool ResolveLabels()
    {
        Dictionary<string, CSLineToken> labelLines = new Dictionary<string, CSLineToken>();
        var imLines = 0;
        foreach (CSLineToken line in Token)
        {
            line.MappedToLine = imLines;
            imLines += line.Intermediate.Length;
            if(line.LineType == CSLineToken.TokenType.Label)
                labelLines.Add(line.Token.First(x => x.Type == CSToken.TokenType.LabelDeclaration).Intermediate, line);
        }

        foreach (var line in Token)
            if(!line.ReplaceLabels(labelLines))
                return false;
        return true;
    }
    
    
}

public struct ChipScriptLine
{
    public bool IsValid;
    public bool WritesToExecutor;
    public bool ReadsFromExecutor;
    public Identifiable LeftId;
    public Identifiable RightId;
    public int LeftIndex;
    public int RightIndex;
    public LogicType RightType;
    public LogicType LeftType;
    public long RightConstant;
    public bool RightIsConstant;

    public static ChipScriptLine From(string intermediate, string original, Identifiable selfId)
    {
        var lr = intermediate.Split('=');
        if (lr.Length != 2)
        {
            Debug.LogError($"Invalid intermediate for {original}: {intermediate}");
            return default;
        }
        
        var left = lr[0].Trim();
        var right = lr[1].Trim();
        
        var csl = new ChipScriptLine();
        if (left.StartsWith("self."))
        {
            csl.WritesToExecutor = true;
            csl.LeftId = selfId;
        }
        else if (left.Split('.').Length < 2)
        {
            Debug.LogError($"Invalid script line '{original}': Must assign to a Parameter");
            return csl;
        }
        else
        {
            var idName = left.Split('.')[0];
            csl.LeftId = Identifiable.FromName(idName);
            if (csl.LeftId == default)
            {
                Debug.LogError($"Invalid script line '{original}': No ID associated with {idName}");
                return csl;
            }
        }

        if (int.TryParse(right, out var constant))
        {
            csl.RightConstant = constant;
            csl.RightType = LogicType.Number;
            csl.RightIsConstant = true;
        }
        else if (!right.Contains('.'))
        {
            var id = Identifiable.FromName(right);
            csl.RightConstant = (long)id.DebugId;
            csl.RightType = LogicType.Type;
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
                csl.RightId = Identifiable.FromName(idName);
                if (csl.RightId == default)
                {
                    Debug.LogError($"Invalid script line '{original}': No ID associated with {idName}");
                    return csl;
                }
            }
            
            csl.RightIndex = -1;
            var pName = right.Split('.')[1];
            var pRight = LogicParametersMappingSystem.ParametersOf(csl.RightId);
            for (int i = 0; i < pRight.Length; i++)
            {
                if (string.Equals(pRight[i].Name.ToString(), pName, StringComparison.InvariantCultureIgnoreCase))
                {
                    csl.RightIndex = i;
                    csl.RightType = pRight[i].Type;
                }
            }

            if (csl.RightIndex < 0)
            {
                Debug.LogError($"Invalid script line '{original}': ID {csl.RightId} has no Parameter {pName}");
                return csl;
            }
        }

        {
            csl.LeftIndex = -1;
            var pName = left.Split('.')[1];
            var pLeft = LogicParametersMappingSystem.ParametersOf(csl.LeftId);
            for (int i = 0; i < pLeft.Length; i++)
            {
                if (string.Equals(pLeft[i].Name.ToString(), pName, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (pLeft[i].Access == LogicAccessType.Read)
                    {
                        Debug.LogError($"Invalid script line '{original}': {pName} on {csl.LeftId} is Read-only");
                        return csl;
                    }
                    csl.LeftIndex = i;
                    csl.LeftType = pLeft[i].Type;
                }
            }

            if (csl.LeftType != csl.RightType)
            {
                Debug.LogError($"Invalid script line '{original}': Type Mismatch {csl.LeftType} / {csl.RightType}");
            }

            if (csl.LeftIndex < 0)
            {
                Debug.LogError($"Invalid script line '{original}': ID {csl.LeftId} has no Parameter {pName}");
                return csl;
            }
        }
        
        csl.IsValid = true;
        return csl;
    }
}