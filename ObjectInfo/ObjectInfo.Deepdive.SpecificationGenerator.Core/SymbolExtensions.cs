using Microsoft.CodeAnalysis;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Core
{
    /// <summary>
    /// Extension methods for working with Roslyn symbols
    /// </summary>
    internal static class SymbolExtensions
    {
        /// <summary>
        /// Determines if the property type is a collection type
        /// </summary>
        public static bool IsCollection(this IPropertySymbol propertySymbol)
        {
            var type = propertySymbol.Type as INamedTypeSymbol;
            if (type == null) return false;

            // Check if it's an array
            if (propertySymbol.Type.TypeKind == TypeKind.Array)
                return true;

            // Check if it implements IEnumerable<T>
            var enumerableInterface = type.AllInterfaces.FirstOrDefault(i =>
                i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);

            return enumerableInterface != null;
        }

        /// <summary>
        /// Gets the element type if the property is a collection, null otherwise
        /// </summary>
        public static ITypeSymbol? GetElementType(this IPropertySymbol propertySymbol)
        {
            var type = propertySymbol.Type as INamedTypeSymbol;
            if (type == null) return null;

            // Handle arrays
            if (propertySymbol.Type.TypeKind == TypeKind.Array)
                return ((IArrayTypeSymbol)propertySymbol.Type).ElementType;

            // Handle generic collections
            var enumerableInterface = type.AllInterfaces.FirstOrDefault(i =>
                i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);

            return enumerableInterface?.TypeArguments.FirstOrDefault();
        }

        /// <summary>
        /// Determines if the type is a simple type (primitive, string, decimal, datetime)
        /// </summary>
        public static bool IsSimpleType(this ITypeSymbol type)
        {
            return type.SpecialType switch
            {
                SpecialType.System_Boolean or
                SpecialType.System_Char or
                SpecialType.System_SByte or
                SpecialType.System_Byte or
                SpecialType.System_Int16 or
                SpecialType.System_UInt16 or
                SpecialType.System_Int32 or
                SpecialType.System_UInt32 or
                SpecialType.System_Int64 or
                SpecialType.System_UInt64 or
                SpecialType.System_Single or
                SpecialType.System_Double or
                SpecialType.System_Decimal or
                SpecialType.System_DateTime or
                SpecialType.System_String => true,
                _ => type.Name == "DateTimeOffset" || type.Name == "Guid"
            };
        }

        /// <summary>
        /// Gets the nullable underlying type if the type is nullable, otherwise returns null
        /// </summary>
        public static ITypeSymbol? GetNullableUnderlyingType(this ITypeSymbol type)
        {
            if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                var nullableType = namedType.ConstructedFrom;
                if (nullableType.SpecialType == SpecialType.System_Nullable_T)
                {
                    return namedType.TypeArguments[0];
                }
            }
            return null;
        }
    }
}