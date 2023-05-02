// ----------------------------------------------------------------------------------
// Copyright (c) The Standard Organization: A coalition of the Good-Hearted Engineers
// ----------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using ObjectInfo.Models.TypeInfo;

namespace ObjectInfo.Models.MethodInfo
{
    public class MethodInfo : IMethodInfo
    {
        public string DeclaringType { get; set; }

        public string Name { get; set; }

        public string ReflectedType { get; set; }
        public List<ITypeInfo> CustomAttrs { get; set; }

    }
}
