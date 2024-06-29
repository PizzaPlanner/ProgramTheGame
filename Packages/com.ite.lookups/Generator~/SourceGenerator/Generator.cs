using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;


namespace Lookups.Generator.SourceGenerator
{
    [Generator]
    public class AssetSourceGenerator : ISourceGenerator
    {
        List<INamedTypeSymbol?> GetAllTypesRecursively(Compilation compilation)
        {
            var types = new List<INamedTypeSymbol?>();
            foreach (var tree in compilation.SyntaxTrees)
            {
                var model = compilation.GetSemanticModel(tree);
                var root = tree.GetRoot();
                var allDeclaredTypes = root.DescendantNodes();
                foreach (var typeDeclaration in allDeclaredTypes.OfType<ClassDeclarationSyntax>())
                    types.Add(model.GetDeclaredSymbol(typeDeclaration));
            }
            return types.Where(x => x != null).ToList();
        }

        string GetFullName(ISymbol symbol, char separator = '.')
        {
            if(symbol is INamedTypeSymbol typeSymbol)
                return typeSymbol.ToDisplayString(NullableFlowState.NotNull, SymbolDisplayFormat.FullyQualifiedFormat);
            string r = symbol.Name;
            while (!string.IsNullOrWhiteSpace(symbol.ContainingNamespace?.Name))
            {
                symbol = symbol.ContainingNamespace;
                r = symbol.Name + separator + r;
            }
            return r;
        }
        
        void CreateSerializers(GeneratorExecutionContext context, INamedTypeSymbol type, INamedTypeSymbol lookupType)
        {
            try
            {
                var source =$@" // Auto-generated code

[assembly:Unity.Entities.RegisterGenericComponentType(typeof({GetFullName(type)}.TComponent))]

{(type.ContainingNamespace != null && !type.ContainingNamespace.Name.StartsWith("<") ? $"namespace {type.ContainingNamespace} {{" : "")}

public partial class {type.Name}
{{
    public partial struct Id: System.IEquatable<Id>
    {{
        public ushort Value;
        
        public bool IsDefault => Value == 0;

        public static Id From(ushort value) => new Id {{ Value = value }};
        public static bool operator ==(Id lhs, Id rhs) => lhs.Value == rhs.Value;
        public static bool operator !=(Id lhs, Id rhs) => lhs.Value != rhs.Value;
        public static readonly Id Default = new Id {{ Value = 0 }};
        public bool Equals(Id other) => Value == other.Value;
        public override bool Equals(object obj) => obj is Id other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();


{string.Join("\n", lookupType.GetMembers().OfType<IFieldSymbol>().Select(x => x.Type.Name switch {
        "BlobArray" => $"        public {GetFullName(x.Type).Replace("Unity.Entities.BlobArray", "Unity.Collections.NativeArray")} {x.Name} => FromBlob(ref Get(Value).{x.Name});",
        "Entity"    => $"        public {GetFullName(x.Type)} {x.Name}(Unity.Entities.WorldUnmanaged world) => Get(world, Value).{x.Name};" +
                       $"        public {GetFullName(x.Type)} {x.Name}Default => Get(Value).{x.Name};",
        _           => $"        public {GetFullName(x.Type)} {x.Name} => Get(Value).{x.Name};",
}))}
    }}
}}
{(type.ContainingNamespace != null && !type.ContainingNamespace.Name.StartsWith("<") ? "}" : "")}
";
                context.AddSource($"{type.Name}.gen.cs", source);
            }
            catch (Exception e)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                    "CG0002",
                    "CodeGen aborted",
                    $"Error during CodeGen: {e.StackTrace}",
                    "",
                    DiagnosticSeverity.Error,
                    true), Location.None));
            }
        }

        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context)
        {
            var libraryAssetType = context.Compilation.GetTypeByMetadataName("Lookups.LibraryAsset`1")!;
            
            var namedTypes = GetAllTypesRecursively(context.Compilation);
            var lookupTypes = namedTypes.Where(x => InheritsFrom(x, libraryAssetType) 
                                                    && !x.IsAbstract
                                                    && x.BaseType.TypeParameters.Length > 0
                                                    && x.BaseType.TypeParameters[0].Name == "TLookup").ToList();
            foreach (var type in lookupTypes)
                CreateSerializers(context, type, (INamedTypeSymbol)type.BaseType!.TypeArguments[0]);
        }

        bool InheritsFrom(INamedTypeSymbol? symbol, INamedTypeSymbol libraryAssetType)
        {
            return symbol?.BaseType != null && (symbol.BaseType?.Name == libraryAssetType.Name || InheritsFrom(symbol.BaseType, libraryAssetType));
        }
    }
}
