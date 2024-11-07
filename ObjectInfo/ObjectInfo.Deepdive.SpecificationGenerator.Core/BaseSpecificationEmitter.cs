using Microsoft.CodeAnalysis;
using ObjectInfo.Deepdive.SpecificationGenerator.Core.Models;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Core
{
    public abstract class BaseSpecificationEmitter : ISpecificationEmitter
    {
        protected readonly ISourceProductionContext Context;

        protected BaseSpecificationEmitter(ISourceProductionContext context)
        {
            Context = context;
        }

        public abstract string EmitSpecification(SpecificationTarget target);

        protected virtual void ReportDiagnostic(DiagnosticDescriptor descriptor, Location? location = null, params object?[] args)
        {
            Context.ReportDiagnostic(descriptor, location, args);
        }

        protected static string GetAccessibilityKeyword(ISymbol symbol)
        {
            return symbol.DeclaredAccessibility switch
            {
                Accessibility.Public => "public",
                Accessibility.Protected => "protected",
                Accessibility.Private => "private",
                Accessibility.Internal => "internal",
                Accessibility.ProtectedAndInternal => "protected internal",
                _ => "public"
            };
        }
    }


}