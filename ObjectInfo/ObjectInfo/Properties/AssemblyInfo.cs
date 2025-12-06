using System.Runtime.CompilerServices;

// Allow unit test project to access internal types for testing TypeTraits and related internals
[assembly: InternalsVisibleTo("ObjectInfo.Unit.Tests")]
// Allow benchmark project to access internal types for performance measurement
[assembly: InternalsVisibleTo("BenchmarkSuite1")]
