using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace Lookups.Generator.SourceGenerator
{
    [Generator]
    public class AssetSourceGenerator : ISourceGenerator
    {
        List<INamedTypeSymbol> allClassTypes;
        List<INamedTypeSymbol> allStructTypes;
        List<INamedTypeSymbol> allEnumTypes;
        void GetAllTypesRecursively(Compilation compilation)
        {
            
            allClassTypes = new List<INamedTypeSymbol>();
            allStructTypes = new List<INamedTypeSymbol>();
            allEnumTypes = new List<INamedTypeSymbol>();
            foreach (var tree in compilation.SyntaxTrees)
            {
                var model = compilation.GetSemanticModel(tree);
                var root = tree.GetRoot();
                var allDeclaredTypes = root.DescendantNodes();
                foreach(var typeDecl in allDeclaredTypes)
                    if(typeDecl is ClassDeclarationSyntax cds)
                        allClassTypes.Add(model.GetDeclaredSymbol(cds)!);
                    else if(typeDecl is StructDeclarationSyntax sds)
                        allStructTypes.Add(model.GetDeclaredSymbol(sds)!);
                    else if(typeDecl is EnumDeclarationSyntax eds)
                        allEnumTypes.Add(model.GetDeclaredSymbol(eds)!);
            }
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
        
        void CreateModData(GeneratorExecutionContext context)
        {
            try
            {
                var source =$@" // Auto-generated code
using Modding.API;
using Modding.API.Generated;

public struct ModGeneratedData : IModGeneratedData 
{{
    public const string ModName = ""{context.Compilation.AssemblyName}""; 
    public const uint ModHash = {(uint)context.Compilation.AssemblyName.GetHashCode()}; 
}}
";
                context.AddSource($"Mod.gen.cs", source);
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
            if(context.Compilation.ReferencedAssemblyNames.All(x => x.Name != "Modding.Compiler"))
                return;
            
            GetAllTypesRecursively(context.Compilation);
            CreateModData(context);
            CreateTagData(context);
        }

        void CreateTagData(GeneratorExecutionContext context)
        {
            var attr = context.Compilation.GetTypeByMetadataName("Modding.API.Tags.IdTagsAttribute");
            if (attr == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                    "CG0003",
                    "Missing reference",
                    $"Mod assembly {context.Compilation.AssemblyName} is missing reference to Modding.API",
                    "",
                    DiagnosticSeverity.Error,
                    true), Location.None));
                return;
            }
            foreach(var e in allEnumTypes
                        .Where(x => x.GetAttributes()
                                     .Any(y => y.AttributeClass == attr)))
            {
                e.GetMembers().OfType<IFieldSymbol>().First().ConstantValue
                var name = e.ToDisplayString();
                var code = $@"// code-generated
using Modding.API.Tags;

public static class {e.Name}Extensions 
{{
    public static TagData Data(this {name} tag) => new TagData 
    {{
         ProviderHash = ModGeneratedData.ModHash,
         Mask = (uint)tag
    }};  
}}
";
                context.AddSource($"Mod.{e.Name}.gen.cs", code);
            }
        }

        bool InheritsFrom(INamedTypeSymbol? symbol, INamedTypeSymbol libraryAssetType)
        {
            return symbol?.BaseType != null && (symbol.BaseType?.Name == libraryAssetType.Name || InheritsFrom(symbol.BaseType, libraryAssetType));
        }
    }
}
