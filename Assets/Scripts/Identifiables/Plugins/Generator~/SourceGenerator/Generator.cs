using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DefSourceGenerator
{
    [Generator]
    public class DefSourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will gather candidate classes during the compilation.
            context.RegisterForSyntaxNotifications(() => new DefSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var compilation = context.Compilation;
            var defInterface = compilation.GetTypeByMetadataName("Identifiables.IDef");
            
            if (defInterface == null)
                return;
            
            if (context.SyntaxReceiver is not DefSyntaxReceiver receiver)
                return;
            
            var sourcesToAdd = new Dictionary<string, string>();
            
            foreach (var candidate in receiver.CandidateClasses)
            {
                var semanticModel = compilation.GetSemanticModel(candidate.SyntaxTree);
                var typeSymbol = semanticModel.GetDeclaredSymbol(candidate) as INamedTypeSymbol;
            
                if (typeSymbol == null || !typeSymbol.AllInterfaces.Contains(defInterface))
                    continue; // Skip if the class doesn't implement IDef
            
                var generatedCode = GenerateCodeForType(typeSymbol);
                var filename = $"{typeSymbol.Name}.Gen.cs";
            
                sourcesToAdd[filename] = generatedCode;
            }
            
            // Add generated files to the compilation
            foreach (var kvp in sourcesToAdd)
            {
                context.AddSource(kvp.Key, kvp.Value);
            }
        }

        private string GenerateCodeForType(INamedTypeSymbol typeSymbol)
        {
            var className = typeSymbol.Name;
            var namespaceName = typeSymbol.ContainingNamespace.IsGlobalNamespace 
                ? string.Empty 
                : $"namespace {typeSymbol.ContainingNamespace.ToDisplayString()}\n{{";

            return $@"
using Identifiables;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
{namespaceName}
public partial struct {className}
{{        
    partial class MappingSystem : IdentifiableMappingSystem
    {{
        static readonly SharedStatic<{className}> Default = SharedStatic<{className}>.GetOrCreate<MappingSystem>(); 
        static readonly SharedStatic<UnsafeHashMap<Identifiable, BlobAssetReference<{className}>>> DefaultWorldIdMap =
            SharedStatic<UnsafeHashMap<Identifiable, BlobAssetReference<{className}>>>
                .GetOrCreate<{className}>();
        
        internal static ref {className} GetDefOrDefault(Identifiable id) =>  ref (TryGetDef(id, out var value) ? ref value.Value : ref Default.Data);
        internal static bool TryGetDef(Identifiable id, out BlobAssetReference<{className}> value) => DefaultWorldIdMap.Data.TryGetValue(id, out value);
        internal static NativeArray<Identifiable> GetAllDefs() => DefaultWorldIdMap.Data.GetKeyArray(Allocator.Temp);

        protected override void OnStartRunning()
        {{
            UnityEngine.Assertions.Assert.IsFalse(DefaultWorldIdMap.Data.IsCreated, ""Multi-world currently not supported"");
            DefaultWorldIdMap.Data = new (0, Allocator.Persistent);
            UnityEngine.Assertions.Assert.IsTrue(DefaultWorldIdMap.Data.IsCreated);
        }}

        protected override void OnDestroy()
        {{
            // The referenced blob assets are owned by the mods and will not be disposed here
            if(DefaultWorldIdMap.Data.IsCreated)
                DefaultWorldIdMap.Data.Dispose();
            UnityEngine.Assertions.Assert.IsFalse(DefaultWorldIdMap.Data.IsCreated);
        }}
        
        protected void UpdateData(Identifiable id, BlobAssetReference<{className}> def)
        {{
            if(DefaultWorldIdMap.Data.TryGetValue(id, out var old) && old.IsCreated)
                old.Dispose();
            DefaultWorldIdMap.Data[id] = def;
        }}

        protected void DefaultMappingUpdate<T>() where T: unmanaged, IDefRef<{className}>
        {{
            var query = new EntityQueryBuilder(Allocator.Temp)
                        .WithAll<Prefab, T, Identifiable>()
                        .WithOptions(EntityQueryOptions.IncludePrefab)
                        .Build(EntityManager);
            var refs = query.ToComponentDataArray<T>(Allocator.Temp);
            var ids = query.ToComponentDataArray<Identifiable>(Allocator.Temp);
            for (int i = 0; i < refs.Length; i++)
                if (refs[i].Def.IsCreated)
                    DefaultWorldIdMap.Data.Add(ids[i], refs[i].Def);
        }}
    }}

    public struct Id : IEquatable<Identifiable>, IEquatable<Id>
    {{
        Identifiable _id;
        public ref {className} Def => ref MappingSystem.GetDefOrDefault(_id);
    
        public static NativeArray<Identifiable> All => MappingSystem.GetAllDefs();

        Id(Identifiable id)
        {{
            if(Id.Has(id) || id == default)
                _id = id;
#if UNITY_EDITOR
            else if (!Application.isPlaying)
                _id = id;
#endif
            else
            {{
                _id = default; {(className == "BaseDef" ? $"Debug.LogWarning($\"Accessing an unmapped Id\");" : $"Debug.LogWarning($\"{{id}} has no {className}\");" )}
            }}
        }}
        
        public static bool Has(Identifiable id) => MappingSystem.TryGetDef(id, out _);
        
        public static implicit operator Identifiable(Id id) => id._id;
        public static implicit operator Id(Identifiable id) => new Id(id);
        public bool Equals(Identifiable other) => other == _id;
        public bool Equals(Id other) => other._id == _id;
        public override int GetHashCode() => _id.GetHashCode();
        public override bool Equals(object obj) => false;
        public static bool operator ==(Id left, Identifiable right) => left._id == right;
        public static bool operator !=(Id left, Identifiable right) => !(left == right);
        public static bool operator ==(Identifiable left, Id right) => left == right._id;
        public static bool operator !=(Identifiable left, Id right) => !(left == right);
        public static bool operator ==(Id left, Id right) => left._id == right._id;
        public static bool operator !=(Id left, Id right) => !(left == right);

        public override string ToString() => _id.ToString();
    }}
}}

public static class {className}Extensions
{{
    public static bool Is(this Identifiable id)
    {{
        return (Identifiable)(({typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.Id)id) != default;
    }} 
    
    public static ref {typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {className}(this Identifiable id)
    {{
        return ref (({typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.Id)id).Def;
    }}  
}}

{(string.IsNullOrWhiteSpace(namespaceName)? "" : "}")}
";
        }

        // Syntax receiver to gather candidate classes
        private class DefSyntaxReceiver : ISyntaxReceiver
        {
            public List<StructDeclarationSyntax> CandidateClasses { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // Collect class declarations with base types (to check for IDef implementation later)
                if (syntaxNode is StructDeclarationSyntax classDecl)
                {
                    CandidateClasses.Add(classDecl);
                }
            }
        }
    }
}
