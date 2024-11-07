using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using ObjectInfo.Deepdive.SpecificationGenerator.Attributes;
using ObjectInfo.Deepdive.SpecificationGenerator.Core.Emitters;
using ObjectInfo.Deepdive.SpecificationGenerator.Core.Models;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Core
{
    [Generator]
    public class SpecificationGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var syntaxProvider = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    "ObjectInfo.Deepdive.SpecificationGenerator.Attributes.GenerateSpecificationAttribute",
                    predicate: (node, _) => node is ClassDeclarationSyntax,
                    transform: (context, _) => GetSpecificationTarget(context))
                .Where(target => target is not null);

            context.RegisterSourceOutput(syntaxProvider,
                (productionContext, target) => GenerateSource(new SourceProductionContextAdapter(productionContext), target!));

        }

        private static SpecificationTarget? GetSpecificationTarget(GeneratorAttributeSyntaxContext context)
        {
            var classSymbol = context.TargetSymbol as INamedTypeSymbol;
            if (classSymbol == null) return null;

            var attribute = context.Attributes[0];
            var config = new SpecificationConfiguration
            {
                TargetOrm = GetOrmTarget(attribute),
                GenerateNavigationSpecs = GetAttributeBoolValue(attribute, "GenerateNavigationSpecs", true),
                GenerateDocumentation = GetAttributeBoolValue(attribute, "GenerateDocumentation", true),
                GenerateAsyncMethods = GetAttributeBoolValue(attribute, "GenerateAsyncMethods", true),
                BaseClass = GetAttributeTypeValue(attribute, "BaseClass"),
                TargetNamespace = GetAttributeStringValue(attribute, "TargetNamespace")
            };

            var properties = AnalyzeProperties(classSymbol);
            var navigationProperties = config.GenerateNavigationSpecs ?
                AnalyzeNavigationProperties(classSymbol) :
                new List<NavigationPropertyDetails>();

            return new SpecificationTarget(
                classSymbol,
                config,
                properties,
                navigationProperties,
                GetAssemblyConfiguration(classSymbol.ContainingAssembly)
            );
        }

        private static OrmTarget GetOrmTarget(AttributeData attribute)
        {
            var targetValue = attribute.NamedArguments
                .FirstOrDefault(kvp => kvp.Key == "TargetOrm")
                .Value.Value;

            return targetValue is int value ? (OrmTarget)value : OrmTarget.EntityFrameworkCore;
        }

        private static bool GetAttributeBoolValue(AttributeData attribute, string name, bool defaultValue)
        {
            var argument = attribute.NamedArguments
                .FirstOrDefault(kvp => kvp.Key == name)
                .Value.Value;

            return argument is bool value ? value : defaultValue;
        }

        private static string? GetAttributeStringValue(AttributeData attribute, string name)
        {
            var argument = attribute.NamedArguments
                .FirstOrDefault(kvp => kvp.Key == name)
                .Value.Value;

            return argument as string;
        }

        private static INamedTypeSymbol? GetAttributeTypeValue(AttributeData attribute, string name)
        {
            var argument = attribute.NamedArguments
                .FirstOrDefault(kvp => kvp.Key == name)
                .Value.Value;

            return argument as INamedTypeSymbol;
        }

        private static List<PropertyDetails> AnalyzeProperties(INamedTypeSymbol typeSymbol)
        {
            var properties = new List<PropertyDetails>();
            foreach (var member in typeSymbol.GetMembers().OfType<IPropertySymbol>())
            {
                if (member.IsStatic || member.IsIndexer) continue;

                properties.Add(new PropertyDetails(
                    member,
                    new PropertyConfiguration
                    {
                        GenerateContains = member.Type.SpecialType == SpecialType.System_String,
                        GenerateStartsWith = member.Type.SpecialType == SpecialType.System_String,
                        GenerateEndsWith = member.Type.SpecialType == SpecialType.System_String,
                        CaseSensitive = false,
                        GenerateRange = IsRangeableType(member.Type),
                        GenerateNullChecks = member.NullableAnnotation == NullableAnnotation.Annotated
                    }
                ));
            }
            return properties;
        }

        private static List<NavigationPropertyDetails> AnalyzeNavigationProperties(INamedTypeSymbol typeSymbol)
        {
            var navProperties = new List<NavigationPropertyDetails>();
            foreach (var member in typeSymbol.GetMembers().OfType<IPropertySymbol>())
            {
                if (member.IsStatic || member.IsIndexer) continue;
                if (member.Type.TypeKind != TypeKind.Class || IsSystemType(member.Type)) continue;

                navProperties.Add(new NavigationPropertyDetails(
                    member,
                    (INamedTypeSymbol)member.Type,
                    member.IsCollection(),
                    member.NullableAnnotation == NullableAnnotation.Annotated
                ));
            }
            return navProperties;
        }

        private static AssemblyConfiguration GetAssemblyConfiguration(IAssemblySymbol assembly)
        {
            return new AssemblyConfiguration
            {
                DefaultNamespace = "Specifications",
                DefaultGenerateAsync = true,
                DefaultGenerateDocumentation = true,
                DefaultGenerateNavigationSpecs = true,
                DefaultStringComparison = "OrdinalIgnoreCase"
            };
        }

        private static void GenerateSource(ISourceProductionContext context, SpecificationTarget target)
        {
            try
            {
                var emitters = new Dictionary<OrmTarget, ISpecificationEmitter>
        {
            { OrmTarget.EntityFrameworkCore, new EfCoreSpecificationEmitter(context) },
            { OrmTarget.Dapper, new DapperSpecificationEmitter(context) }
        };

                if (target.Configuration.TargetOrm == OrmTarget.Both)
                {
                    GenerateSpecification(context, target, emitters[OrmTarget.EntityFrameworkCore], "EfCore");
                    GenerateSpecification(context, target, emitters[OrmTarget.Dapper], "Dapper");
                }
                else
                {
                    GenerateSpecification(context, target, emitters[target.Configuration.TargetOrm]);
                }

                if (target.Configuration.GenerateNavigationSpecs)
                {
                    foreach (var navProp in target.NavigationProperties)
                    {
                        GenerateNestedSpecification(context, target, navProp, emitters);
                    }
                }
            }
            catch (Exception ex)
            {
                var diagnostic = CreateExceptionDiagnostic(ex, target);
                context.ReportDiagnostic(diagnostic);
            }
        }


        private static void GenerateSpecification(
            ISourceProductionContext context,
            SpecificationTarget target,
            ISpecificationEmitter emitter,
            string? suffix = null)
        {
            var className = target.ClassSymbol.Name + "Specification" + (suffix != null ? $"_{suffix}" : "");
            var sourceText = emitter.EmitSpecification(target);
            var fileName = $"{className}.g.cs";

            context.AddSource(fileName, sourceText);
            //context.AddSource(fileName, SourceText.From(sourceText, Encoding.UTF8));
        }
        

        private static void GenerateNestedSpecification(
            ISourceProductionContext context,
            SpecificationTarget target,
            NavigationPropertyDetails navProp,
            Dictionary<OrmTarget, ISpecificationEmitter> emitters)
        {
            var nestedTarget = new SpecificationTarget(
                navProp.TypeSymbol,
                new SpecificationConfiguration
                {
                    TargetOrm = target.Configuration.TargetOrm,
                    GenerateNavigationSpecs = true,
                    GenerateDocumentation = target.Configuration.GenerateDocumentation,
                    GenerateAsyncMethods = target.Configuration.GenerateAsyncMethods,
                    BaseClass = target.Configuration.BaseClass,
                    TargetNamespace = target.Configuration.TargetNamespace
                },
                AnalyzeProperties(navProp.TypeSymbol),
                AnalyzeNavigationProperties(navProp.TypeSymbol),
                target.AssemblyConfiguration
            );

            if (target.Configuration.TargetOrm == OrmTarget.Both)
            {
                GenerateSpecification(context, nestedTarget, emitters[OrmTarget.EntityFrameworkCore], "EfCore");
                GenerateSpecification(context, nestedTarget, emitters[OrmTarget.Dapper], "Dapper");
            }
            else
            {
                GenerateSpecification(context, nestedTarget, emitters[target.Configuration.TargetOrm]);
            }
        }

        private static string DetermineNamespace(SpecificationTarget target)
        {
            return target.Configuration.TargetNamespace ??
                   $"{target.ClassSymbol.ContainingNamespace}.{target.AssemblyConfiguration.DefaultNamespace}";
        }

        private static bool IsRangeableType(ITypeSymbol type)
        {
            return type.SpecialType switch
            {
                SpecialType.System_Int16 or
                SpecialType.System_Int32 or
                SpecialType.System_Int64 or
                SpecialType.System_Single or
                SpecialType.System_Double or
                SpecialType.System_Decimal or
                SpecialType.System_DateTime => true,
                _ => false
            };
        }

        private static bool IsSystemType(ITypeSymbol type)
        {
            return type.ContainingNamespace?.ToString().StartsWith("System") ?? false;
        }

        private static Diagnostic CreateExceptionDiagnostic(Exception ex, SpecificationTarget target)
        {
            var descriptor = new DiagnosticDescriptor(
                id: "SG001",
                title: "Specification Generation Failed",
                messageFormat: "Failed to generate specification for {0}: {1}",
                category: "SpecificationGenerator",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            return Diagnostic.Create(
                descriptor,
                Location.None,
                target.ClassSymbol.Name,
                ex.Message);
        }
    }

    internal interface ISpecificationEmitter
    {
        string EmitSpecification(SpecificationTarget target);
    }


}