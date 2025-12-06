#region Copyright (c) The Rebel Alliance
// ----------------------------------------------------------------------------------
// Copyright (c) The Rebel Alliance
//⠀⠀⠀⠀⠀⠀⠀⠀⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠠⠀⠀⠀⡇⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠈⠳⣴⣿⠄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠒⠒⠒⠒⠒⢺⢿⣿⢗⠒⠒⠒⠒⠒⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠁⣸⣿⣦⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⢀⣾⡟⠋⢹⣷⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⢀⣿⡟⣴⣶⡄⣿⣧⡄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⢰⣿⣿⣧⢻⣿⣿⣿⣿⡟⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠈⢻⣿⣿⣷⣿⣿⣿⣿⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⠿⣿⣿⣿⣿⣿⣿⣦⣤⣀⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⣾⠀⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣶⣤⡀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⣿⣏⣻⣿⣿⣿⣿⣿⠋⣿⣿⣿⣿⣿⠙⣿⣷⣶⣤⣤⡄
//⠀⠀⠀⠀⢻⢇⣿⣿⣿⣿⣿⠹⠀⢹⣿⣿⣿⡇⠀⢟⣿⣿⡿⠋⠀
//⠀⠀⠀⠀⢘⣼⣿⣿⣿⣿⣿⡆⠀⢸⣿⠛⣿⡇⠀⢸⡿⠋⠀⠀⠀
//⠀⠀⠀⠀⣾⣿⣿⣿⣿⣿⣿⣿⣦⣈⠻⠴⠟⣁⣴⣿⣿⠗⠀⠀⠀
//⠀⠀⠀⢸⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⠋⠀⠀⠀⠀
//⠀⠀⢀⣿⣿⠻⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⠟⠁⠀⠀⠀⠀⠀
//⠀⠀⣾⣿⡏⠀⠹⣿⠿⠿⠿⠿⣿⣿⣿⠿⠛⠁⠀⠀⠀⠀⠀⠀⠀
//⠀⢰⣿⡿⠀⠀⠀⠀⠀⠀⠀⠀⠀⢿⣿⡄⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⣿⣿⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠘⣿⣧⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⣰⣿⡏⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢿⣿⣄⠀⠀⠀⠀⠀⠀⠀⠀
//⠉⠉⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠉⠉⠀
// ---------------------------------------------------------------------------------- 
#endregion


using FluentAssertions;
using ObjectInfo.Brokers.ObjectInfo;
using ObjectInfo.Models.ObjectInfo;
using static ObjectInfo.Unit.Tests.ObjectInfoService;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace ObjectInfo.Unit.Tests
{
    public class ObjectInfoBrokerTest
    {
        [Fact]
        public void ShouldRetrieveObjectInfo()
        {
            // given            
            TestClass testClass = new TestClass() { Name = "Joe The Tester" };
            TestClass testClass2 = new TestClass() { Name = "Joe The Tester" };
            TestClass testClass3 = new TestClass() { Name = "Joe The Chef" };

            IObjectInfoBroker objectInfoBroker = new ObjectInfoBroker();

            ObjInfo expectedObjectInfo = ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, testClass);

            // when
            var actualObjectInfo = ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, testClass2);
            var actualObjectInfo3 = ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, testClass3);

            // then
            actualObjectInfo.Should().BeEquivalentTo(expectedObjectInfo);
            actualObjectInfo3.Should().NotBeEquivalentTo(expectedObjectInfo);
        }

        [Fact]
        public void ShouldNavigateFieldInfo()
        {
            // given            
            TestClassWithFields testClass = new TestClassWithFields();
            IObjectInfoBroker objectInfoBroker = new ObjectInfoBroker();
            ObjInfo? objectInfo = ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, testClass);

            // when
            var constField = objectInfo!.TypeInfo!.FieldInfos!.FirstOrDefault(f => f.Name == "ConstField");
            var readOnlyField = objectInfo!.TypeInfo!.FieldInfos!.FirstOrDefault(f => f.Name == "ReadOnlyField");

            // then
            constField.Should().NotBeNull();
            constField!.IsConstant.Should().BeTrue();
            constField!.Value.Should().Be("Const Value");

            readOnlyField.Should().NotBeNull();
            readOnlyField!.IsReadOnly.Should().BeTrue();
            readOnlyField!.Value.Should().Be("ReadOnly Value");
        }

        [Fact]
        public void ShouldNavigateFieldAttributes()
        {
            // given            
            TestClassWithFields testClass = new TestClassWithFields();
            IObjectInfoBroker objectInfoBroker = new ObjectInfoBroker();
            ObjInfo? objectInfo = ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, testClass);

            // when
            var annotatedField = objectInfo!.TypeInfo!.FieldInfos!.FirstOrDefault(f => f.Name == "AnnotatedField");
            var attribute = annotatedField!.CustomAttrs!.FirstOrDefault(a => a.Name.Equals("IsCompliant"));

            // then
            attribute.Should().NotBeNull();
            attribute!.Name.Should().Be("IsCompliant");
        }

        [Fact]
        public void ShouldNavigateGenericTypeInfo()
        {
            // given            
            TestGenericClass<string> testClass = new TestGenericClass<string>() { Value = "Test" };
            IObjectInfoBroker objectInfoBroker = new ObjectInfoBroker();
            ObjInfo? objectInfo = ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, testClass);

            // when
            var genericType = objectInfo!.TypeInfo;

            // then
            genericType.Should().NotBeNull();
            genericType!.IsConstructedGenericType.Should().BeTrue();
            genericType!.GenericTypeArguments.Should().ContainSingle();
            genericType!.GenericTypeArguments.First().Name.Should().Be("String");
        }

        [Fact]
        public void ShouldNavigateGenericConstraints()
        {
            // given            
            Type genericTypeDefinition = typeof(TestConstrainedGenericClass<>); // Get generic type definition directly
            IObjectInfoBroker objectInfoBroker = new ObjectInfoBroker();

            // when - create ObjectInfo from the type definition itself
            ObjInfo? objectInfo = ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, genericTypeDefinition);
            var genericTypeInfo = objectInfo!.TypeInfo;

            // then
            genericTypeInfo.Should().NotBeNull();
            genericTypeInfo.IsGenericTypeDefinition.Should().BeTrue();
            genericTypeInfo.GenericParameters.Should().NotBeEmpty("Generic type should have type parameters");

            var genericParam = genericTypeInfo.GenericParameters.FirstOrDefault();
            genericParam.Should().NotBeNull("Should have at least one generic parameter");

            // Check constraints using our model's properties
            genericParam!.HasDefaultConstructorConstraint.Should().BeTrue("Should have new() constraint");
            genericParam.Constraints.Should().Contain(c => c.Name == "ITestConstraint",
                "Should have ITestConstraint constraint");

            // Additional verification
            genericParam.Position.Should().Be(0, "Should be the first generic parameter");
            genericParam.Name.Should().Be("T", "Generic parameter should be named 'T'");
        }

        [Fact]
        public void ShouldNavigateEventInfo()
        {
            // given            
            TestClassWithEvents testClass = new TestClassWithEvents();
            IObjectInfoBroker objectInfoBroker = new ObjectInfoBroker();
            ObjInfo? objectInfo = ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, testClass);

            // when
            var simpleEvent = objectInfo!.TypeInfo!.EventInfos!.FirstOrDefault(e => e.Name == "SimpleEvent");
            var customEvent = objectInfo!.TypeInfo!.EventInfos!.FirstOrDefault(e => e.Name == "CustomEvent");

            // then
            simpleEvent.Should().NotBeNull();
            simpleEvent!.EventHandlerType.Should().Contain("EventHandler");
            simpleEvent!.IsMulticast.Should().BeTrue();

            customEvent.Should().NotBeNull();
            customEvent!.IsMulticast.Should().BeTrue();
            customEvent!.AddMethod.Should().NotBeNull();
            customEvent!.RemoveMethod.Should().NotBeNull();
        }

        [Fact]
        public void ShouldNavigateInterfaceInfo()
        {
            // given            
            TestClass testClass = new TestClass() { Name = "Joe The Tester" };
            IObjectInfoBroker objectInfoBroker = new ObjectInfoBroker();
            ObjInfo? expectedObjectInfo = ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, testClass);

            // when
            var implementedInterface = expectedObjectInfo!.TypeInfo!.ImplementedInterfaces!.FirstOrDefault(a => a.Name.Equals("ITestClass"));
            string? expectedImplementedInterface = implementedInterface?.Name;

            // then
            expectedImplementedInterface.Should().NotBe(null);
            expectedImplementedInterface.Should().Be("ITestClass");
        }

        [Fact]
        public void ShouldNavigateMethodInfo()
        {
            // given            
            TestClass testClass = new TestClass() { Name = "Joe The Tester" };
            IObjectInfoBroker objectInfoBroker = new ObjectInfoBroker();
            ObjInfo? expectedObjectInfo = ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, testClass);

            // when
            var methodInfo = expectedObjectInfo!.TypeInfo!.MethodInfos!.FirstOrDefault(a => a.Name.Equals("EnsureCompliance"));
            string? expectedMethodInfo = methodInfo?.Name;

            // then
            expectedMethodInfo.Should().NotBe(null);
            expectedMethodInfo.Should().Be("EnsureCompliance");
        }

        [Fact]
        public void ShouldNavigatePropInfo()
        {
            // given            
            TestClass testClass = new TestClass() { Name = "Joe The Tester" };
            IObjectInfoBroker objectInfoBroker = new ObjectInfoBroker();
            ObjInfo? expectedObjectInfo = ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, testClass);

            // when
            var propInfo = expectedObjectInfo!.TypeInfo!.PropInfos!.FirstOrDefault(a => a.Name.Equals("Name"));
            string? expectedPropInfo = propInfo?.Name;

            // then
            expectedPropInfo.Should().NotBe(null);
            expectedPropInfo.Should().Be("Name");
        }

        [Fact]
        public void ShouldNavigateConstructorInfo()
        {
            // given            
            TestClassWithConstructor testClass = new TestClassWithConstructor("Joe The Tester");
            IObjectInfoBroker objectInfoBroker = new ObjectInfoBroker();
            ObjInfo? objectInfo = ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, testClass);

            // when
            var constructor = objectInfo!.TypeInfo!.ConstructorInfos!.FirstOrDefault();

            // then
            constructor.Should().NotBeNull();
            constructor!.ParameterTypes.Should().ContainSingle().Which.Should().Be("String");
            constructor.ParameterNames.Should().ContainSingle().Which.Should().Be("name");
            constructor.IsPublic.Should().BeTrue();
            constructor.IsStatic.Should().BeFalse();
            constructor.DeclaringType.Should().Be("TestClassWithConstructor");
        }

        [Fact]
        public void ShouldNavigateConstructorAttributeInfo()
        {
            // given            
            TestClassWithAnnotatedConstructor testClass = new TestClassWithAnnotatedConstructor("Joe The Tester");
            IObjectInfoBroker objectInfoBroker = new ObjectInfoBroker();
            ObjInfo? objectInfo = ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, testClass);

            // when
            var constructor = objectInfo!.TypeInfo!.ConstructorInfos!.FirstOrDefault();
            var attribute = constructor!.CustomAttrs!.FirstOrDefault(a => a.Name.Equals("IsCompliant"));

            // then
            attribute.Should().NotBeNull();
            attribute!.Name.Should().Be("IsCompliant");
        }

        [Fact]
        public void ShouldNavigateTypeAttributeInfo()
        {
            // given            
            TestClass testClass = new TestClass() { Name = "Joe The Tester" };
            IObjectInfoBroker objectInfoBroker = new ObjectInfoBroker();
            ObjInfo? expectedObjectInfo = ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, testClass);

            // when
            var attrInfo = expectedObjectInfo!.TypeInfo!.CustomAttrs!.FirstOrDefault(a => a.Name.Equals("IsCompliant"));
            string? expectedAttrInfo = attrInfo?.Name;

            // then
            expectedAttrInfo.Should().NotBe(null);
            expectedAttrInfo.Should().Be("IsCompliant");
        }

        [Fact]
        public void ShouldNavigatePropertyAttributeInfo()
        {
            // given            
            TestClass testClass = new TestClass() { Name = "Joe The Tester" };
            IObjectInfoBroker objectInfoBroker = new ObjectInfoBroker();
            ObjInfo? expectedObjectInfo = ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, testClass);

            // when
            var expectedPropInfo =
                expectedObjectInfo!.TypeInfo!.PropInfos!.FirstOrDefault(a => a.Name.Equals("Name"));
            var attrInfo = expectedPropInfo?.CustomAttrs?.FirstOrDefault(a => a.Name.Equals("IsCompliant"));
            string? expectedAttrInfo = attrInfo?.Name;

            // then
            expectedAttrInfo.Should().NotBe(null);
            expectedAttrInfo.Should().Be("IsCompliant");
        }

        [Fact]
        public void ShouldNavigateMethodAttributeInfo()
        {
            // given            
            TestClass testClass = new TestClass() { Name = "Joe The Tester" };
            IObjectInfoBroker objectInfoBroker = new ObjectInfoBroker();
            ObjInfo? expectedObjectInfo = ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, testClass);

            // when
            var expectedMethInfo = expectedObjectInfo?.TypeInfo?.MethodInfos?
                .FirstOrDefault(a => a.Name.Equals("EnsureCompliance"));

            expectedMethInfo.Should().NotBeNull("EnsureCompliance method should exist");

            var expectedAttrInfo = expectedMethInfo?.CustomAttrs?
                .FirstOrDefault(a => a.Name.Equals("IsCompliant"));

            // then
            expectedAttrInfo.Should().NotBeNull("IsCompliant attribute should exist");
            expectedAttrInfo!.Name.Should().Be("IsCompliant");
        }

    }
}
