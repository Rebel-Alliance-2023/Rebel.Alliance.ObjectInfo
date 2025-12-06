using System;
using System.Globalization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ObjectInfo.Infrastructure;
using ObjectInfo.Models.PropInfo;
using Microsoft.VSDiagnostics;

namespace ObjectInfo.Benchmarks
{
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    [CPUUsageDiagnoser]
    public class TypeTraitsPropInfoBenchmarks
    {
        private readonly DateTime _dt = new DateTime(2024, 12, 25, 10, 30, 0);
        private readonly string _dateOnlyStr = "2024-12-25";
        private readonly string _dateTimeLocalStr = "2024-12-25T10:30";
        private readonly CultureInfo _inv = CultureInfo.InvariantCulture;
        private readonly decimal _dec = 1234.56m;
        private readonly double _dbl = 1234.56;
        private readonly float _flt = 1234.56f;
        private readonly string _intStr = "123456";
        private readonly string _longStr = "1234567890123";
        private readonly string _decStr = "123456.78";
        private readonly string _dblStr = "123456.78";
        private readonly string _fltStr = "123456.78";
        private PropInfo _propInfoDateTime;
        [GlobalSetup]
        public void Setup()
        {
            _propInfoDateTime = new PropInfo
            {
                Value = _dt
            };
        }

        [Benchmark]
        public string FormatForInput_Date_Default() => TypeTraits<DateTime>.FormatForInput(_dt, kindOverride: null, _inv);
        [Benchmark]
        public string FormatForInput_DateTimeLocal() => TypeTraits<DateTime>.FormatForInput(_dt, kindOverride: "DateTimeLocal", _inv);
        [Benchmark]
        public string FormatForInput_Decimal() => TypeTraits<decimal>.FormatForInput(_dec, kindOverride: null, _inv);
        [Benchmark]
        public string FormatForInput_Double() => TypeTraits<double>.FormatForInput(_dbl, kindOverride: null, _inv);
        [Benchmark]
        public bool TryParseFromEventValue_Date()
        {
            return TypeTraits<DateTime>.TryParseFromEventValue(_dateOnlyStr, _inv, out DateTime parsed);
        }

        [Benchmark]
        public bool TryParseFromEventValue_DateTimeLocal()
        {
            return TypeTraits<DateTime>.TryParseFromEventValue(_dateTimeLocalStr, _inv, out DateTime parsed);
        }

        [Benchmark]
        public string PropInfo_GetFormattedValue_Default() => _propInfoDateTime.GetFormattedValue(kindOverride: null, _inv);
        [Benchmark]
        public string PropInfo_GetFormattedValue_DateTimeLocal() => _propInfoDateTime.GetFormattedValue(kindOverride: "DateTimeLocal", _inv);

        [Benchmark]
        public bool TryParseFromEventValue_Int32()
        {
            return TypeTraits<int>.TryParseFromEventValue(_intStr, _inv, out int parsed);
        }

        [Benchmark]
        public bool TryParseFromEventValue_Int64()
        {
            return TypeTraits<long>.TryParseFromEventValue(_longStr, _inv, out long parsed);
        }

        [Benchmark]
        public bool TryParseFromEventValue_Decimal()
        {
            return TypeTraits<decimal>.TryParseFromEventValue(_decStr, _inv, out decimal parsed);
        }

        [Benchmark]
        public bool TryParseFromEventValue_Double()
        {
            return TypeTraits<double>.TryParseFromEventValue(_dblStr, _inv, out double parsed);
        }

        [Benchmark]
        public bool TryParseFromEventValue_Float()
        {
            return TypeTraits<float>.TryParseFromEventValue(_fltStr, _inv, out float parsed);
        }
    }
}