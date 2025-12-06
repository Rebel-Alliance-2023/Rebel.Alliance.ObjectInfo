using System;
using System.Globalization;
using Xunit;
using ObjectInfo.Infrastructure;
using ObjectInfo.Models.PropInfo;

namespace ObjectInfo.Unit.Tests
{
    public class TypeTraitsTests
    {
        [Fact]
        public void DateTime_FormatForInput_Default_IsDate()
        {
            var dt = new DateTime(2024, 12, 25, 10, 30, 0);
            var s = TypeTraits<DateTime>.FormatForInput(dt, kindOverride: null, CultureInfo.InvariantCulture);
            Assert.Equal("2024-12-25", s);
        }

        [Fact]
        public void DateTime_FormatForInput_Local_IncludesTime()
        {
            var dt = new DateTime(2024, 12, 25, 10, 30, 0);
            var s = TypeTraits<DateTime>.FormatForInput(dt, kindOverride: "DateTimeLocal", CultureInfo.InvariantCulture);
            Assert.Equal("2024-12-25T10:30", s);
        }

        [Fact]
        public void Numeric_FormatForInput_IsInvariant()
        {
            var dec = 1234.56m;
            var sDec = TypeTraits<decimal>.FormatForInput(dec, null, CultureInfo.InvariantCulture);
            Assert.Equal("1234.56", sDec);

            var dbl = 1234.56;
            var sDbl = TypeTraits<double>.FormatForInput(dbl, null, CultureInfo.InvariantCulture);
            Assert.Equal("1234.56", sDbl);
        }

        private enum MyEnum { Alpha, Beta }

        [Fact]
        public void Enum_ToOptionValueString_ReturnsName()
        {
            var s = TypeTraits<MyEnum>.ToOptionValueString(MyEnum.Beta, CultureInfo.InvariantCulture);
            Assert.Equal("Beta", s);
        }

        [Fact]
        public void BuildEnumOptions_ReturnsPairs()
        {
            var options = TypeTraits<MyEnum>.BuildEnumOptions();
            Assert.Collection(options,
                o => { Assert.Equal(MyEnum.Alpha, o.Value); Assert.Equal("Alpha", o.Name); },
                o => { Assert.Equal(MyEnum.Beta, o.Value); Assert.Equal("Beta", o.Name); }
            );
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData("on", true)]
        public void TryParseFromEventValue_Bool(string input, bool expected)
        {
            bool parsed;
            var ok = TypeTraits<bool>.TryParseFromEventValue(input, CultureInfo.InvariantCulture, out parsed);
            Assert.True(ok);
            Assert.Equal(expected, parsed);
        }

        [Fact]
        public void TryParseFromEventValue_DateTime_Exact()
        {
            DateTime parsed;
            var ok = TypeTraits<DateTime>.TryParseFromEventValue("2024-12-25", CultureInfo.InvariantCulture, out parsed);
            Assert.True(ok);
            Assert.Equal(new DateTime(2024, 12, 25), parsed.Date);

            ok = TypeTraits<DateTime>.TryParseFromEventValue("2024-12-25T10:30", CultureInfo.InvariantCulture, out parsed);
            Assert.True(ok);
            Assert.Equal(10, parsed.Hour);
            Assert.Equal(30, parsed.Minute);
        }

        [Fact]
        public void PropInfo_GetFormattedValue_UsesTypeTraits()
        {
            var p = new PropInfo { Value = new DateTime(2024, 12, 25, 10, 30, 0) };
            var s = p.GetFormattedValue(kindOverride: null, CultureInfo.InvariantCulture);
            Assert.Equal("2024-12-25", s);

            s = p.GetFormattedValue(kindOverride: "DateTimeLocal", CultureInfo.InvariantCulture);
            Assert.Equal("2024-12-25T10:30", s);
        }
    }
}
