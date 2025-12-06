using Microsoft.CodeAnalysis;
using ObjectInfo.Deepdive.SpecificationGenerator.Core.Models;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Core
{
    /// <summary>
    /// Base class for specification emitters that generate source code.
    /// </summary>
    public abstract class BaseSpecificationEmitter : ISpecificationEmitter
    {
        /// <summary>
        /// The source production context for emitting generated code.
        /// </summary>
        protected readonly ISourceProductionContext Context;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseSpecificationEmitter"/> class.
        /// </summary>
        /// <param name="context">The source production context.</param>
        protected BaseSpecificationEmitter(ISourceProductionContext context)
        {
            Context = context;
        }

        /// <summary>
        /// Emits the specification source code for the target.
        /// </summary>
        /// <param name="target">The specification target.</param>
        /// <returns>The generated source code.</returns>
        public abstract string EmitSpecification(SpecificationTarget target);

        /// <summary>
        /// Reports a diagnostic to the source production context.
        /// </summary>
        /// <param name="descriptor">The diagnostic descriptor.</param>
        /// <param name="location">The optional location.</param>
        /// <param name="args">The message arguments.</param>
        protected virtual void ReportDiagnostic(DiagnosticDescriptor descriptor, Location? location = null, params object?[] args)
        {
            Context.ReportDiagnostic(descriptor, location, args);
        }

        /// <summary>
        /// Gets the accessibility keyword for a symbol.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>The accessibility keyword.</returns>
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