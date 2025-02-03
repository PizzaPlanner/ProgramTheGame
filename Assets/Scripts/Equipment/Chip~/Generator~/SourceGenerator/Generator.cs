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
        List<INamedTypeSymbol> GetAllTypesRecursively(Compilation compilation)
        {
            var types = new List<INamedTypeSymbol>();
            foreach (var tree in compilation.SyntaxTrees)
            {
                var model = compilation.GetSemanticModel(tree);
                var root = tree.GetRoot();
                var allDeclaredTypes = root.DescendantNodes();
                foreach (var typeDeclaration in allDeclaredTypes.OfType<StructDeclarationSyntax>())
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
        
        void CreateSerializers(GeneratorExecutionContext context, INamedTypeSymbol type)
        {
            var ns = type.ContainingNamespace.ToDisplayString();
            var fullName = GetFullName(type);
            var allFields = type.GetMembers().Where(x => x.Kind == SymbolKind.Field).ToArray();
            var readonlyFields = allFields.Where(x => !x.GetAttributes().Any());
            var writeFields = allFields
                .Where(x => x.GetAttributes().Any(attr => attr.AttributeClass.Name == "LogicWritableAttribute"))
                .Select(x => (x, Array.IndexOf(allFields, x)));
            try
            {
                var source =$@"// Auto-generated code
using Programmable;
using System.Runtime.InteropServices;
using Unity.Entities;
using Unity.Burst;

{(!ns.StartsWith("<") ? $"namespace {ns} {{" : "")}

[StructLayout(LayoutKind.Sequential)]
public unsafe partial struct {type.Name} : IChipTargetableImpl<{type.Name}>
{{
    public void ApplyReadDataTo(ref {type.Name} to)
    {{
        {string.Join("\n        ", readonlyFields.Select(field => $"to.{field.Name} = {field.Name};"))}
    }}
    
    public {type.Name} ApplyWriteDataFrom({type.Name} from, DynamicBuffer<ChipAccessibleData> data, int initialOffset)
    {{
        {string.Join("\n        ", writeFields.Select(f => $"if(data.WasWrittenTo({f.Item2})) {f.x.Name} = from.{f.x.Name};"))}
        return this;        
    }}
}}

{(!ns.StartsWith("<") ? $"}}" : "")}
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
            var chipTargetableType = context.Compilation.GetTypeByMetadataName("Programmable.IChipTargetable")!;
            
            var namedTypes = GetAllTypesRecursively(context.Compilation);
            var lookupTypes = namedTypes.Where(x => x.AllInterfaces.Any(itf => itf.Name == chipTargetableType.Name)).ToList(); 
            
            foreach (var type in lookupTypes)
                CreateSerializers(context, type);
        }
    }
}
