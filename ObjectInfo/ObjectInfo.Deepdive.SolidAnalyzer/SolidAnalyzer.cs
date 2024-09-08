using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ObjectInfo.DeepDive.Analyzers;
using ObjectInfo.DeepDive.Analysis;
using ObjectInfo.Models.TypeInfo;
using ObjectInfo.Models.ObjectInfo;
using ObjectInfo.Models.MethodInfo;
using ObjectInfo.Models.PropInfo;
using Serilog;

namespace ObjectInfo.Deepdive.SolidAnalyzer
{
    public class SolidAnalyzer : IAnalyzer
    {
        private readonly ILogger _logger;
        private readonly SolidAnalyzerConfig? _config;

        public string Name => "SOLID Principles Analyzer";

        public SolidAnalyzer(ILogger logger, SolidAnalyzerConfig? config = null)
        {
            _logger = logger;
            _config = config ?? new SolidAnalyzerConfig();
        }

        public async Task<AnalysisResult> AnalyzeAsync(AnalysisContext context)
        {
            _logger.Information($"SolidAnalyzer.AnalyzeAsync started for type: {context.Target?.GetType().Name}");

            if (context.Target is ObjInfo objInfo)
            {
                return await AnalyzeTypeAsync(objInfo.TypeInfo);
            }
            else if (context.Target is ITypeInfo typeInfo)
            {
                return await AnalyzeTypeAsync(typeInfo);
            }
            else
            {
                _logger.Warning($"Invalid analysis target type: {context.Target?.GetType().Name}");
                return new AnalysisResult(Name, "Invalid analysis target", "The provided context does not contain a valid ObjInfo or ITypeInfo instance.");
            }
        }

        private async Task<TypeAnalysisResult> AnalyzeTypeAsync(ITypeInfo typeInfo)
        {
            _logger.Information($"Analyzing type: {typeInfo.Name}");

            var result = new SolidAnalysisResult(Name, typeInfo.Name, "SOLID Analysis Complete", "");

            result.SingleResponsibilityAnalysis = AnalyzeSrp(typeInfo);
            result.OpenClosedAnalysis = AnalyzeOcp(typeInfo);
            result.LiskovSubstitutionAnalysis = AnalyzeLsp(typeInfo);
            result.InterfaceSegregationAnalysis = AnalyzeIsp(typeInfo);
            result.DependencyInversionAnalysis = AnalyzeDip(typeInfo);

            result.Details = result.ToString();

            return await Task.FromResult(result);
        }

        private SrpAnalysis AnalyzeSrp(ITypeInfo typeInfo)
        {
            var publicMembers = typeInfo.MethodInfos.Count() + typeInfo.PropInfos.Count();

            var analysis = new SrpAnalysis
            {
                PublicMemberCount = publicMembers,
                Violations = new List<string>()
            };

            if (publicMembers > _config.MaxMethodsPerClass)
            {
                analysis.Violations.Add($"Class has {publicMembers} public members, which exceeds the recommended maximum of {_config.MaxMethodsPerClass}.");
            }

            return analysis;
        }

        private OcpAnalysis AnalyzeOcp(ITypeInfo typeInfo)
        {
            var analysis = new OcpAnalysis
            {
                IsAbstract = typeInfo.IsAbstract,
                VirtualMethodCount = typeInfo.MethodInfos.Count(m => m.IsVirtual),
                Violations = new List<string>()
            };

            _logger.Information($"Analyzing type {typeInfo.Name}: IsAbstract={analysis.IsAbstract}, VirtualMethodCount={analysis.VirtualMethodCount}");

            foreach (var method in typeInfo.MethodInfos)
            {
                _logger.Information($"Method {method.Name} is virtual: {method.IsVirtual}");
            }

            if (!analysis.IsAbstract && analysis.VirtualMethodCount == 0)
            {
                analysis.Violations.Add("Class is not abstract and contains no virtual methods, potentially violating OCP.");
            }

            return analysis;
        }

        private LspAnalysis AnalyzeLsp(ITypeInfo typeInfo)
        {
            var assembly = System.Reflection.Assembly.Load(typeInfo.Assembly);
            var types = assembly.GetTypes();
            var type = types.Where(a => a.Name.Contains(typeInfo.Name)).FirstOrDefault();
            var methods = type.GetMethods();

            var analysis = new LspAnalysis
            {
                Violations = new List<string>()
            };

            if (typeInfo.BaseType != null)
            {
                var baseType = types.Where(a => a.Name.Contains(typeInfo.BaseType)).FirstOrDefault();
                //assembly.GetType(typeInfo.BaseType);
                if (baseType != null)
                    foreach (System.Reflection.MethodInfo method in methods)
                    {
                        var baseMethod = baseType?.GetMethod(method.Name);
                        if (baseMethod != null && method.GetBaseDefinition() != baseMethod)
                        {
                            analysis.Violations.Add($"Method {method.Name} may violate LSP. Manual review recommended.");
                        }
                    }
            }

            //if (typeInfo.BaseType != null)
            //{
            //    foreach (System.Reflection.MethodInfo method in methods)
            //    {
            //        //if (method.Name.StartsWith("override", StringComparison.OrdinalIgnoreCase))
            //        // how can we tell if a method is an override?

            //        var baseMethod = typeInfo.BaseType.MethodInfos.FirstOrDefault(m => m.Name == method.Name);
            //        if (method.)
            //        {
            //            analysis.Violations.Add($"Method {method.Name} may violate LSP. Manual review recommended.");
            //        }
            //    }
            //}

            return analysis;
        }

        private IspAnalysis AnalyzeIsp(ITypeInfo typeInfo)
        {
            var analysis = new IspAnalysis
            {
                InterfaceCount = typeInfo.ImplementedInterfaces?.Count() ?? 0,
                Violations = new List<string>()
            };

            if (typeInfo.ImplementedInterfaces != null)
            {
                foreach (var iface in typeInfo.ImplementedInterfaces)
                {
                    if (iface.MethodInfos != null)
                    {
                        _logger.Information($"Analyzing interface {iface.Name} with {iface.MethodInfos.Count()} methods.");
                        var implementedMethods = typeInfo.MethodInfos.Select(m => m.Name).ToHashSet();
                        foreach (var method in iface.MethodInfos)
                        {
                            if (!implementedMethods.Contains(method.Name))
                            {
                                analysis.Violations.Add($"Class does not fully implement interface {iface.Name}. Missing method: {method.Name}");
                            }
                        }
                    }
                    else
                    {
                        _logger.Warning($"Interface {iface.Name} has null MethodInfos.");
                    }
                }
            }
            else
            {
                _logger.Warning("ImplementedInterfaces is null.");
            }

            return analysis;
        }

        private DipAnalysis AnalyzeDip(ITypeInfo typeInfo)
        {
            var analysis = new DipAnalysis
            {
                DependencyCount = 0,
                AbstractDependencyCount = 0,
                Violations = new List<string>()
            };

            var assembly = System.Reflection.Assembly.Load(typeInfo.Assembly);
            var types = assembly.GetTypes();
            var type = types.Where(a => a.Name.Contains(typeInfo.Name)).FirstOrDefault();
            //var methods = type.GetMethods();

            //Given a type, how to you get its contructors?
            var constructors = type.GetConstructors();



            // Analyze properties
            foreach (var prop in typeInfo.PropInfos)
            {
                analysis.DependencyCount++;
                if (IsAbstractType(prop.PropertyType))
                {
                    analysis.AbstractDependencyCount++;
                }
                else
                {
                    analysis.Violations.Add($"Property {prop.Name} is a concrete type ({prop.PropertyType}), potentially violating DIP.");
                }
            }

            // Analyze constructors
            //var constructors = typeInfo.MethodInfos.Where(m => m.Name == typeInfo.Name);
            //var constructors = methods.Where(m => m.Name == typeInfo.Name);

            foreach (var ctor in constructors)
            {
                foreach (var param in ctor.GetParameters())
                {
                    analysis.DependencyCount++;
                    if (param.ParameterType.IsInterface)
                    {
                        analysis.AbstractDependencyCount++;
                    }
                    else
                    {
                        analysis.Violations.Add($"Constructor parameter of type {param} is a concrete type, potentially violating DIP.");
                    }
                }
            }

            return analysis;
        }

        private bool IsAbstractType(string typeName)
        {
            return typeName.StartsWith("interface", StringComparison.OrdinalIgnoreCase) ||
                   typeName.StartsWith("abstract", StringComparison.OrdinalIgnoreCase) ||
                   typeName.EndsWith("base", StringComparison.OrdinalIgnoreCase);
        }


    }

    public class SolidAnalysisResult : TypeAnalysisResult
    {
        public SrpAnalysis SingleResponsibilityAnalysis { get; set; }
        public OcpAnalysis OpenClosedAnalysis { get; set; }
        public LspAnalysis LiskovSubstitutionAnalysis { get; set; }
        public IspAnalysis InterfaceSegregationAnalysis { get; set; }
        public DipAnalysis DependencyInversionAnalysis { get; set; }

        public SolidAnalysisResult(string analyzerName, string typeName, string summary, string details)
            : base(analyzerName, typeName, summary, details, 0, 0)
        {
            SingleResponsibilityAnalysis = new SrpAnalysis();
            OpenClosedAnalysis = new OcpAnalysis();
            LiskovSubstitutionAnalysis = new LspAnalysis();
            InterfaceSegregationAnalysis = new IspAnalysis();
            DependencyInversionAnalysis = new DipAnalysis();
        }

        public override string ToString()
        {
            return $"SOLID Analysis Results for {TypeName}:\n\n" +
                   $"Single Responsibility Principle:\n{SingleResponsibilityAnalysis}\n\n" +
                   $"Open-Closed Principle:\n{OpenClosedAnalysis}\n\n" +
                   $"Liskov Substitution Principle:\n{LiskovSubstitutionAnalysis}\n\n" +
                   $"Interface Segregation Principle:\n{InterfaceSegregationAnalysis}\n\n" +
                   $"Dependency Inversion Principle:\n{DependencyInversionAnalysis}";
        }
    }

    public class SrpAnalysis
    {
        public int PublicMemberCount { get; set; }
        public List<string> Violations { get; set; } = new List<string>();

        public override string ToString() =>
            $"Public Members: {PublicMemberCount}\nViolations: {string.Join(", ", Violations)}";
    }

    public class OcpAnalysis
    {
        public bool IsAbstract { get; set; }
        public int VirtualMethodCount { get; set; }
        public List<string> Violations { get; set; } = new List<string>();

        public override string ToString() =>
            $"Is Abstract: {IsAbstract}\nVirtual Methods: {VirtualMethodCount}\nViolations: {string.Join(", ", Violations)}";
    }

    public class LspAnalysis
    {
        public List<string> Violations { get; set; } = new List<string>();

        public override string ToString() =>
            $"Violations: {string.Join(", ", Violations)}";
    }

    public class IspAnalysis
    {
        public int InterfaceCount { get; set; }
        public List<string> Violations { get; set; } = new List<string>();

        public override string ToString() =>
            $"Interfaces Implemented: {InterfaceCount}\nViolations: {string.Join(", ", Violations)}";
    }

    public class DipAnalysis
    {
        public int DependencyCount { get; set; }
        public int AbstractDependencyCount { get; set; }
        public List<string> Violations { get; set; } = new List<string>();

        public override string ToString() =>
            $"Total Dependencies: {DependencyCount}\nAbstract Dependencies: {AbstractDependencyCount}\nViolations: {string.Join(", ", Violations)}";
    }

    public class SolidAnalyzerConfig
    {
        public int MaxMethodsPerClass { get; set; } = 10;
        public bool AnalyzeSrp { get; set; } = true;
        public bool AnalyzeOcp { get; set; } = true;
        public bool AnalyzeLsp { get; set; } = true;
        public bool AnalyzeIsp { get; set; } = true;
        public bool AnalyzeDip { get; set; } = true;
    }
}
