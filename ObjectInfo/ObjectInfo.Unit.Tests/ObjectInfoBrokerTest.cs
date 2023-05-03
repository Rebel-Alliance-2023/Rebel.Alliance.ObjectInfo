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
            var actualObjectInfo =
                ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, testClass2);
            var actualObjectInfo3 =
                ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, testClass3);

            // then
            actualObjectInfo.Should().BeEquivalentTo(expectedObjectInfo);
            actualObjectInfo3.Should().NotBeEquivalentTo(expectedObjectInfo);
        }
        [Fact]
        public void ShouldNavigateInterfaceInfo()
        {
            // given            
            TestClass testClass = new TestClass() { Name = "Joe The Tester" };
            IObjectInfoBroker objectInfoBroker = new ObjectInfoBroker();
            ObjInfo? expectedObjectInfo = ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, testClass);
            // when
            string? expectedImplementedInterface =
                expectedObjectInfo!.TypeInfo!.ImplementedInterfaces!.FirstOrDefault(a => a.Name.Equals("ITestClass")).Name;
            // then
            expectedImplementedInterface.Should().NotBe(null);
            expectedImplementedInterface.Equals("ITestClass");

        }
        [Fact]
        public void ShouldNavigateMethodInfo()
        {
            // given            
            TestClass testClass = new TestClass() { Name = "Joe The Tester" };
            IObjectInfoBroker objectInfoBroker = new ObjectInfoBroker();
            ObjInfo? expectedObjectInfo = ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, testClass);
            // when
            string? expectedMethodInfo =
                expectedObjectInfo!.TypeInfo!.MethodInfos!.FirstOrDefault(a => a.Name.Equals("EnsureCompliance")).Name;
            // then
            expectedMethodInfo.Should().NotBe(null);
            expectedMethodInfo.Equals("EnsureCompliance");

        }
        [Fact]
        public void ShouldNavigatePropInfo()
        {
            // given            
            TestClass testClass = new TestClass() { Name = "Joe The Tester" };
            IObjectInfoBroker objectInfoBroker = new ObjectInfoBroker();
            ObjInfo? expectedObjectInfo = ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, testClass);
            // when
            string? expectedPropInfo =
                expectedObjectInfo!.TypeInfo!.PropInfos!.FirstOrDefault(a => a.Name.Equals("Name")).Name;
            // then
            expectedPropInfo.Should().NotBe(null);
            expectedPropInfo.Equals("Name");

        }
        [Fact]
        public void ShouldNavigateTypeAttributeInfo()
        {
            // given            
            TestClass testClass = new TestClass() { Name = "Joe The Tester" };
            IObjectInfoBroker objectInfoBroker = new ObjectInfoBroker();
            ObjInfo? expectedObjectInfo = ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, testClass);
            // when
            string? expectedAttrInfo =
                expectedObjectInfo!.TypeInfo!.CustomAttrs!.FirstOrDefault(a => a.Name.Equals("IsCompliant")).Name;
            // then
            expectedAttrInfo.Should().NotBe(null);
            expectedAttrInfo.Equals("IsCompliant");

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
            string? expectedAttrInfo =
                expectedPropInfo.CustomAttrs!.FirstOrDefault(a => a.Name.Equals("IsCompliant")).Name;
            // then
            expectedAttrInfo.Should().NotBe(null);
            expectedAttrInfo.Equals("IsCompliant");

        }
        [Fact]
        public void ShouldNavigateMethodAttributeInfo()
        {
            // given            
            TestClass testClass = new TestClass() { Name = "Joe The Tester" };
            IObjectInfoBroker objectInfoBroker = new ObjectInfoBroker();
            ObjInfo? expectedObjectInfo = ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, testClass);
            // when
            var expectedMethInfo =
                expectedObjectInfo!.TypeInfo!.MethodInfos!.FirstOrDefault(a => a.Name.Equals("EnsureCompliance"));
            string? expectedAttrInfo =
                expectedMethInfo.CustomAttrs!.FirstOrDefault(a => a.Name.Equals("IsCompliant")).Name;
            // then
            expectedAttrInfo.Should().NotBe(null);
            expectedAttrInfo.Equals("IsCompliant");

        }
    }
}