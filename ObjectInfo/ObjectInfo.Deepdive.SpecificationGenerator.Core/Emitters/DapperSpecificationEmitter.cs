﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using SpecialType = Microsoft.CodeAnalysis.SpecialType;
using ObjectInfo.Deepdive.SpecificationGenerator.Core.Models;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Core.Emitters
{
    public class DapperSpecificationEmitter : BaseSpecificationEmitter
    {
        public DapperSpecificationEmitter(ISourceProductionContext context) : base(context) { }

        public override string EmitSpecification(SpecificationTarget target)
        {
            var builder = new StringBuilder();

            // Add file header and usings
            EmitFileHeader(builder, target);
            EmitUsings(builder);
            builder.AppendLine();

            // Begin namespace
            builder.AppendLine($"namespace {DetermineNamespace(target)}");
            builder.AppendLine("{");

            // Begin class
            var accessibility = GetAccessibilityKeyword(target.ClassSymbol);
            var baseType = GetBaseTypeName(target);
            builder.AppendLine($"    {accessibility} class {target.ClassSymbol.Name}Specification : {baseType}");
            builder.AppendLine("    {");

            // Private fields for parameter handling
            EmitPrivateFields(builder);

            // Properties for filtering
            EmitFilterProperties(builder, target);

            // SQL generation methods
            EmitSqlGenerationMethods(builder, target);

            // Parameter generation methods
            EmitParameterMethods(builder, target);

            // Navigation property methods
            if (target.Configuration.GenerateNavigationSpecs)
            {
                EmitNavigationPropertyMethods(builder, target);
            }

            // Query execution methods
            EmitQueryMethods(builder, target);

            // Close class and namespace
            builder.AppendLine("    }");
            builder.AppendLine("}");

            return builder.ToString();
        }

        private void EmitFileHeader(StringBuilder builder, SpecificationTarget target)
        {
            if (target.Configuration.GenerateDocumentation)
            {
                builder.AppendLine("// <auto-generated/>");
                builder.AppendLine("// Generated by ObjectInfo.Deepdive.SpecificationGenerator");
                builder.AppendLine($"// Target Entity: {target.ClassSymbol.Name}");
                builder.AppendLine($"// Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                builder.AppendLine();
            }
        }

        private void EmitUsings(StringBuilder builder)
        {
            builder.AppendLine("using System;");
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine("using System.Data;");
            builder.AppendLine("using System.Linq;");
            builder.AppendLine("using System.Text;");
            builder.AppendLine("using System.Threading;");
            builder.AppendLine("using System.Threading.Tasks;");
            builder.AppendLine("using Dapper;");
            builder.AppendLine();
        }

        private string GetBaseTypeName(SpecificationTarget target)
        {
            var entityType = target.ClassSymbol.Name;
            return target.Configuration.BaseClass?.Name ?? $"SqlSpecification<{entityType}>";
        }

        private void EmitPrivateFields(StringBuilder builder)
        {
            builder.AppendLine("        private readonly StringBuilder _whereBuilder = new();");
            builder.AppendLine("        private readonly Dictionary<string, object> _parameters = new();");
            builder.AppendLine("        private int _parameterIndex;");
            builder.AppendLine();
        }

        private void EmitFilterProperties(StringBuilder builder, SpecificationTarget target)
        {
            foreach (var property in target.Properties)
            {
                var propertyType = property.Symbol.Type;
                var propertyName = property.Symbol.Name;
                var config = property.Configuration;

                // Main property value
                if (config.GenerateNullChecks && propertyType.NullableAnnotation == NullableAnnotation.Annotated)
                {
                    EmitNullableProperty(builder, propertyName, propertyType);
                }
                else
                {
                    EmitStandardProperty(builder, propertyName, propertyType);
                }

                // String-specific filters
                if (propertyType.SpecialType == SpecialType.System_String && config.GenerateContains)
                {
                    EmitStringSearchProperties(builder, propertyName, config);
                }

                // Range filters for numeric and datetime types
                if (config.GenerateRange && IsRangeableType(propertyType))
                {
                    EmitRangeProperties(builder, propertyName, propertyType);
                }

                builder.AppendLine();
            }

            // Paging properties
            builder.AppendLine("        public int? PageNumber { get; set; }");
            builder.AppendLine("        public int? PageSize { get; set; }");
            builder.AppendLine();
        }

        private void EmitSqlGenerationMethods(StringBuilder builder, SpecificationTarget target)
        {
            // Base select query
            builder.AppendLine("        protected virtual string GetBaseQuery()");
            builder.AppendLine("        {");
            builder.AppendLine($"            return \"SELECT * FROM [{target.ClassSymbol.Name}]\";");
            builder.AppendLine("        }");
            builder.AppendLine();

            // Build where clause
            builder.AppendLine("        protected virtual void BuildWhereClause()");
            builder.AppendLine("        {");
            builder.AppendLine("            _whereBuilder.Clear();");
            builder.AppendLine("            _parameters.Clear();");
            builder.AppendLine("            _parameterIndex = 0;");
            builder.AppendLine();

            foreach (var property in target.Properties)
            {
                EmitPropertyWhereClause(builder, property);
            }

            builder.AppendLine("        }");
            builder.AppendLine();

            // ToSql override
            builder.AppendLine("        public override string ToSql()");
            builder.AppendLine("        {");
            builder.AppendLine("            BuildWhereClause();");
            builder.AppendLine();
            builder.AppendLine("            var sql = new StringBuilder(GetBaseQuery());");
            builder.AppendLine();
            builder.AppendLine("            if (_whereBuilder.Length > 0)");
            builder.AppendLine("            {");
            builder.AppendLine("                sql.Append(\" WHERE \").Append(_whereBuilder);");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            if (PageSize.HasValue && PageNumber.HasValue)");
            builder.AppendLine("            {");
            builder.AppendLine("                sql.Append($\" OFFSET {(PageNumber.Value - 1) * PageSize.Value} ROWS\");");
            builder.AppendLine("                sql.Append($\" FETCH NEXT {PageSize.Value} ROWS ONLY\");");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            return sql.ToString();");
            builder.AppendLine("        }");
            builder.AppendLine();
        }

        private void EmitPropertyWhereClause(StringBuilder builder, PropertyDetails property)
        {
            var propertyName = property.Symbol.Name;
            var propertyType = property.Symbol.Type;
            var config = property.Configuration;

            // Null checks
            if (config.GenerateNullChecks)
            {
                builder.AppendLine($"            if ({propertyName}IsNull.HasValue)");
                builder.AppendLine("            {");
                builder.AppendLine($"                AddWhereClause($\"[{propertyName}] IS {(property.Configuration.GenerateNullChecks ? "NOT " : "")}NULL\");");
                builder.AppendLine("            }");
                builder.AppendLine();
            }

            // Main property value
            builder.AppendLine($"            if (_{propertyName} != null)");
            builder.AppendLine("            {");
            builder.AppendLine($"                AddParameterizedWhereClause($\"[{propertyName}] = @{propertyName}\", $\"@{propertyName}\", _{propertyName});");
            builder.AppendLine("            }");
            builder.AppendLine();

            // String-specific operations
            if (propertyType.SpecialType == SpecialType.System_String && config.GenerateContains)
            {
                EmitStringWhereClause(builder, propertyName, config);
            }

            // Range operations
            if (config.GenerateRange && IsRangeableType(propertyType))
            {
                EmitRangeWhereClause(builder, propertyName);
            }
        }

        private void EmitParameterMethods(StringBuilder builder, SpecificationTarget target)
        {
            builder.AppendLine("        private void AddWhereClause(string clause)");
            builder.AppendLine("        {");
            builder.AppendLine("            if (_whereBuilder.Length > 0)");
            builder.AppendLine("            {");
            builder.AppendLine("                _whereBuilder.Append(\" AND \");");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            _whereBuilder.Append(clause);");
            builder.AppendLine("        }");
            builder.AppendLine();

            builder.AppendLine("        private void AddParameterizedWhereClause(string clause, string parameterName, object value)");
            builder.AppendLine("        {");
            builder.AppendLine("            AddWhereClause(clause);");
            builder.AppendLine("            _parameters.Add(parameterName, value);");
            builder.AppendLine("        }");
            builder.AppendLine();

            builder.AppendLine("        public override DynamicParameters GetParameters()");
            builder.AppendLine("        {");
            builder.AppendLine("            var parameters = new DynamicParameters();");
            builder.AppendLine("            foreach (var param in _parameters)");
            builder.AppendLine("            {");
            builder.AppendLine("                parameters.Add(param.Key, param.Value);");
            builder.AppendLine("            }");
            builder.AppendLine("            return parameters;");
            builder.AppendLine("        }");
            builder.AppendLine();
        }

        private void EmitQueryMethods(StringBuilder builder, SpecificationTarget target)
        {
            var entityType = target.ClassSymbol.Name;

            if (target.Configuration.GenerateAsyncMethods)
            {
                // Single async
                builder.AppendLine($"        public async Task<{entityType}?> FirstOrDefaultAsync(IDbConnection connection, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)");
                builder.AppendLine("        {");
                builder.AppendLine("            return await connection.QueryFirstOrDefaultAsync<TEntity>(");
                builder.AppendLine("                ToSql(),");
                builder.AppendLine("                GetParameters(),");
                builder.AppendLine("                transaction");
                builder.AppendLine("            );");
                builder.AppendLine("        }");
                builder.AppendLine();

                // Multiple async
                builder.AppendLine($"        public async Task<IEnumerable<{entityType}>> QueryAsync(IDbConnection connection, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)");
                builder.AppendLine("        {");
                builder.AppendLine("            return await connection.QueryAsync<TEntity>(");
                builder.AppendLine("                ToSql(),");
                builder.AppendLine("                GetParameters(),");
                builder.AppendLine("                transaction");
                builder.AppendLine("            );");
                builder.AppendLine("        }");
                builder.AppendLine();

                // Count async
                builder.AppendLine("        public override async Task<int> GetCountAsync(IDbConnection connection, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)");
                builder.AppendLine("        {");
                builder.AppendLine("            var countSql = $\"SELECT COUNT(*) FROM [{entityType}]\"");
                builder.AppendLine("            if (_whereBuilder.Length > 0)");
                builder.AppendLine("            {");
                builder.AppendLine("                countSql += $\" WHERE {_whereBuilder}\";");
                builder.AppendLine("            }");
                builder.AppendLine();
                builder.AppendLine("            return await connection.ExecuteScalarAsync<int>(");
                builder.AppendLine("                countSql,");
                builder.AppendLine("                GetParameters(),");
                builder.AppendLine("                transaction");
                builder.AppendLine("            );");
                builder.AppendLine("        }");
            }
        }

        private void EmitNavigationPropertyMethods(StringBuilder builder, SpecificationTarget target)
        {
            foreach (var navProp in target.NavigationProperties)
            {
                var propName = navProp.Symbol.Name;
                var typeName = navProp.TypeSymbol.Name;

                builder.AppendLine($"        public {typeName}Specification {propName}Specification {{ get; private set; }}");
                builder.AppendLine();

                builder.AppendLine($"        public {target.ClassSymbol.Name}Specification Include{propName}(Action<{typeName}Specification> specificationAction)");
                builder.AppendLine("        {");
                builder.AppendLine($"            {propName}Specification = new {typeName}Specification();");
                builder.AppendLine($"            specificationAction({propName}Specification);");
                builder.AppendLine("            return this;");
                builder.AppendLine("        }");
                builder.AppendLine();
            }
        }

        private void EmitNullableProperty(StringBuilder builder, string propertyName, ITypeSymbol propertyType)
        {
            builder.AppendLine($"        private {propertyType}? _{propertyName};");
            builder.AppendLine($"        public {propertyType}? {propertyName}");
            builder.AppendLine("        {");
            builder.AppendLine($"            get => _{propertyName};");
            builder.AppendLine("            set");
            builder.AppendLine("            {");
            builder.AppendLine($"                _{propertyName} = value;");
            builder.AppendLine("            }");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine($"        public bool? {propertyName}IsNull {{ get; set; }}");
        }

        private void EmitStandardProperty(StringBuilder builder, string propertyName, ITypeSymbol propertyType)
        {
            builder.AppendLine($"        private {propertyType} _{propertyName};");
            builder.AppendLine($"        public {propertyType} {propertyName}");
            builder.AppendLine("        {");
            builder.AppendLine($"            get => _{propertyName};");
            builder.AppendLine("            set");
            builder.AppendLine("            {");
            builder.AppendLine($"                _{propertyName} = value;");
            builder.AppendLine("            }");
            builder.AppendLine("        }");
        }

        private void EmitStringSearchProperties(StringBuilder builder, string propertyName, PropertyConfiguration config)
        {
            if (config.GenerateContains)
            {
                builder.AppendLine($"        public string? {propertyName}Contains {{ get; set; }}");
            }
            if (config.GenerateStartsWith)
            {
                builder.AppendLine($"        public string? {propertyName}StartsWith {{ get; set; }}");
            }
            if (config.GenerateEndsWith)
            {
                builder.AppendLine($"        public string? {propertyName}EndsWith {{ get; set; }}");
            }
        }

        private void EmitRangeProperties(StringBuilder builder, string propertyName, ITypeSymbol propertyType)
        {
            builder.AppendLine($"        public {propertyType}? {propertyName}From {{ get; set; }}");
            builder.AppendLine($"        public {propertyType}? {propertyName}To {{ get; set; }}");
        }


        private void EmitStringWhereClause(StringBuilder builder, string propertyName, PropertyConfiguration config)
        {
            var comparisonOp = config.CaseSensitive ? "LIKE" : "LIKE";
            var columnExpr = config.CaseSensitive ? $"[{propertyName}]" : $"LOWER([{propertyName}])";

            if (config.GenerateContains)
            {
                builder.AppendLine($"            if (!string.IsNullOrEmpty({propertyName}Contains))");
                builder.AppendLine("            {");
                // Use string concatenation instead of string interpolation since we're generating code
                builder.AppendLine($"                string paramName = $\"@{propertyName}Contains\" + _parameterIndex++;");
                if (config.CaseSensitive)
                {
                    builder.AppendLine($"                AddParameterizedWhereClause($\"{columnExpr} {comparisonOp} '%' + {{paramName}} + '%'\", paramName, {propertyName}Contains);");
                }
                else
                {
                    builder.AppendLine($"                AddParameterizedWhereClause($\"{columnExpr} {comparisonOp} '%' + {{paramName}} + '%'\", paramName, {propertyName}Contains.ToLower());");
                }
                builder.AppendLine("            }");
                builder.AppendLine();
            }

            if (config.GenerateStartsWith)
            {
                builder.AppendLine($"            if (!string.IsNullOrEmpty({propertyName}StartsWith))");
                builder.AppendLine("            {");
                builder.AppendLine($"                string paramName = $\"@{propertyName}StartsWith\" + _parameterIndex++;");
                if (config.CaseSensitive)
                {
                    builder.AppendLine($"                AddParameterizedWhereClause($\"{columnExpr} {comparisonOp} {{paramName}} + '%'\", paramName, {propertyName}StartsWith);");
                }
                else
                {
                    builder.AppendLine($"                AddParameterizedWhereClause($\"{columnExpr} {comparisonOp} {{paramName}} + '%'\", paramName, {propertyName}StartsWith.ToLower());");
                }
                builder.AppendLine("            }");
                builder.AppendLine();
            }

            if (config.GenerateEndsWith)
            {
                builder.AppendLine($"            if (!string.IsNullOrEmpty({propertyName}EndsWith))");
                builder.AppendLine("            {");
                builder.AppendLine($"                string paramName = $\"@{propertyName}EndsWith\" + _parameterIndex++;");
                if (config.CaseSensitive)
                {
                    builder.AppendLine($"                AddParameterizedWhereClause($\"{columnExpr} {comparisonOp} '%' + {{paramName}}\", paramName, {propertyName}EndsWith);");
                }
                else
                {
                    builder.AppendLine($"                AddParameterizedWhereClause($\"{columnExpr} {comparisonOp} '%' + {{paramName}}\", paramName, {propertyName}EndsWith.ToLower());");
                }
                builder.AppendLine("            }");
                builder.AppendLine();
            }
        }

        private void EmitRangeWhereClause(StringBuilder builder, string propertyName)
        {
            builder.AppendLine($"            if ({propertyName}From.HasValue)");
            builder.AppendLine("            {");
            builder.AppendLine($"                string paramName = $\"@{propertyName}From\" + _parameterIndex++;");
            builder.AppendLine($"                AddParameterizedWhereClause($\"[{propertyName}] >= {{paramName}}\", paramName, {propertyName}From.Value);");
            builder.AppendLine("            }");
            builder.AppendLine();

            builder.AppendLine($"            if ({propertyName}To.HasValue)");
            builder.AppendLine("            {");
            builder.AppendLine($"                string paramName = $\"@{propertyName}To\" + _parameterIndex++;");
            builder.AppendLine($"                AddParameterizedWhereClause($\"[{propertyName}] <= {{paramName}}\", paramName, {propertyName}To.Value);");
            builder.AppendLine("            }");
            builder.AppendLine();
        }


        private string DetermineNamespace(SpecificationTarget target)
        {
            return target.Configuration.TargetNamespace ??
                   $"{target.ClassSymbol.ContainingNamespace}.{target.AssemblyConfiguration.DefaultNamespace}";
        }

        private bool IsRangeableType(ITypeSymbol type)
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
    }
}