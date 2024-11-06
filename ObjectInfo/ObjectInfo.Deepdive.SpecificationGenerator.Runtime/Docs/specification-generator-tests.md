# SpecificationGenerator Test Suite Plan

## 1. Attribute Tests

### GenerateSpecificationAttributeTests
- `Constructor_SetsDefaultValues_Correctly()`
- `TargetOrm_DefaultsToEntityFrameworkCore()`
- `GenerateNavigationSpecs_DefaultsToTrue()`
- `BaseClass_IsNullByDefault()`
- `CustomConfiguration_AppliesCorrectly()`

### SpecificationPropertyAttributeTests
- `Constructor_SetsDefaultValues_Correctly()`
- `Ignore_DefaultsToFalse()`
- `StringOperations_DefaultToTrue()`
- `CaseSensitive_DefaultsToFalse()`
- `CustomExpression_CanBeSet()`

## 2. Generator Core Tests

### SpecificationGeneratorTests
- `Initialize_RegistersCorrectSyntaxProvider()`
- `GenerateSource_CreatesValidCSharpCode()`
- `AttributeDetection_WorksWithPartialClasses()`
- `AttributeDetection_WorksWithNestedClasses()`
- `MultipleTargets_GeneratesCorrectFiles()`

### PropertyAnalysisTests
- `AnalyzeProperties_IdentifiesAllPropertyTypes()`
- `AnalyzeProperties_HandlesNullableTypes()`
- `AnalyzeProperties_HandlesCollections()`
- `AnalyzeProperties_RespectsIgnoreAttribute()`
- `AnalyzeProperties_HandlesDifferentAccessLevels()`

### NavigationPropertyTests
- `AnalyzeNavigation_IdentifiesRelationships()`
- `AnalyzeNavigation_HandlesCollectionTypes()`
- `AnalyzeNavigation_HandlesSelfReferences()`
- `AnalyzeNavigation_RespectsDepthLimit()`
- `AnalyzeNavigation_HandlesCircularReferences()`

## 3. Emitter Tests

### EFCoreSpecificationEmitterTests
- `EmitSpecification_GeneratesValidClass()`
- `EmitFilterProperties_GeneratesCorrectProperties()`
- `EmitApplyCriteria_GeneratesCorrectLogic()`
- `EmitConstructors_IncludesAllVariants()`
- `EmitIncludeConfigurations_HandlesAllRelationships()`
- `EmitNavigation_GeneratesCorrectMethods()`

### DapperSpecificationEmitterTests
- `EmitSpecification_GeneratesValidClass()`
- `EmitSqlGeneration_ProducesValidSql()`
- `EmitParameterHandling_WorksCorrectly()`
- `EmitQueryMethods_IncludesAllOperations()`
- `EmitAsyncMethods_GeneratesCorrectly()`

## 4. Runtime Tests

### BaseSpecificationTests
- `Criteria_DefaultsToAllTrue()`
- `And_CombinesSpecificationsCorrectly()`
- `Or_CombinesSpecificationsCorrectly()`
- `Not_NegatesSpecificationCorrectly()`
- `IsSatisfiedBy_EvaluatesCorrectly()`
- `Includes_WorkAsExpected()`
- `OrderBy_AppliesCorrectly()`
- `Paging_WorksAsExpected()`

### SqlSpecificationTests
- `ToSql_GeneratesValidSql()`
- `GetParameters_ReturnsCorrectParameters()`
- `WhereClause_BuildsCorrectly()`
- `Combination_WorksWithAndOperator()`
- `Combination_WorksWithOrOperator()`
- `Negation_GeneratesCorrectSql()`

## 5. Integration Tests

### EFCoreIntegrationTests
- `ApplySpecification_GeneratesCorrectQuery()`
- `NavigationProperties_LoadCorrectly()`
- `Filtering_WorksWithAllPropertyTypes()`
- `Ordering_AppliesCorrectly()`
- `Paging_WorksWithLargeDatasets()`
- `ComplexSpecifications_ExecuteCorrectly()`

### DapperIntegrationTests
- `QueryWithSpecification_ReturnsCorrectResults()`
- `FirstOrDefault_WorksAsExpected()`
- `Count_ReturnsCorrectNumber()`
- `Parameters_HandleAllTypes()`
- `ComplexQueries_ExecuteCorrectly()`
- `AsyncOperations_WorkCorrectly()`

## 6. Performance Tests

### GeneratorPerformanceTests
- `GenerateSpecifications_CompletesWithinTimeout()`
- `HandleLargeEntitySets_CompletesWithinTimeout()`
- `MemoryUsage_StaysWithinBounds()`

### RuntimePerformanceTests
- `LargeDataSet_QueryPerformance()`
- `ComplexSpecification_ExecutionTime()`
- `MultipleIncludes_PerformanceImpact()`
- `ConcurrentQueries_HandleEfficiently()`

## 7. Error Handling Tests

### ValidationTests
- `InvalidAttribute_ReportsError()`
- `InvalidPropertyType_ReportsError()`
- `CircularDependency_HandledGracefully()`
- `InvalidSyntax_ProducesUnderstandableError()`
- `MissingDependency_ReportsError()`

### ErrorRecoveryTests
- `PartialGeneration_ContinuesAfterError()`
- `InvalidConfiguration_FallsBackToDefaults()`
- `MalformedInput_HandledGracefully()`

## Test Implementation Guidelines

1. **Setup Requirements**
   - Use xUnit test frameworks
   - Implement appropriate test fixtures
   - Use in-memory databases where applicable
   - Implement proper cleanup in test teardown

2. **Test Data**
   - Create comprehensive test entities
   - Include edge cases
   - Use realistic data volumes
   - Cover all supported property types

3. **Assertions**
   - Verify generated code compiles
   - Check for correct runtime behavior
   - Validate performance metrics
   - Ensure proper error handling

4. **Coverage Goals**
   - Aim for >90% code coverage
   - Cover all public APIs
   - Include negative test cases
   - Test boundary conditions
