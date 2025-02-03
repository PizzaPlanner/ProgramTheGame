using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public class LogicChip : IComponentData
{
    public string Location;
    public Guid UniqueId;
    [Multiline(40)]
    public string Script;

    public ChipScriptParser.Error LastError;

    // public bool ToIntermediate(out string[] lines)
    // {
    //     var labelLines = new Dictionary<string, int>();
    //     var list = Script.Split('\n').Select(x => x.Split('#')[0].Trim()).Where(x => x.Length > 0).ToList();
    //     for (var i = 0; i < list.Count; i++)
    //     {
    //         var line = list[i];
    //         if(ProcessLabel(line))
    //         {
    //             labelLines.Add(line[..^1], i);
    //             list.RemoveAt(i--);
    //         }
    //     }
    //     
    //     var intermediate = string.Join('\n', list);
    //     foreach(var label in labelLines)
    //         intermediate = intermediate.Replace($"@{label.Key}", label.Value.ToString());
    //
    //     lines = intermediate.Split('\n');
    //     Debug.Log($"Intermediate script:\n{intermediate}");
    //     return true;
    // }

    // bool ProcessLabel(string line)
    // {
    //     return line.EndsWith(':');
    // }

    public void UpdateScript(string code)
    {
        Script = code;
        LastError = ChipScriptParser.Error.NoError;
    }    
}