[nuget package](https://www.nuget.org/packages/Rebel.Alliance.ObjectInfo)
# Rebel.Alliance.ObjectInfo

![image](https://user-images.githubusercontent.com/3196088/235502858-8f615664-a196-45c8-bb07-df0ec6fc2e2a.png)

https://www.nuget.org/packages/ObjectInfo/1.0.0

Presenting a minimalist library to easily query the DotNet Reflection API which multi-targets .NetStandard2.0 and .NetStandard2.1

The ObjectInfo Broker queries the Reflection API and converts the data from the various internal types to string properties, so that any client can read the data without needing references to hidden or protected libraries. Thus, this library is ideal for developers developing an "Object Inspector" in Blazor for instance.

The top-level object is ObjectInfo, which contains the TypeInfo class, which in turn contains ImplementedInterfaces, PropInfo, MethodInfo. The Type, Method and Property models, each, contain a CustomAttributes collection. Thus, all relevant Reflection meta-data rolls up under ObjectInfo.

ObjectInfo also contains a configuration object. We will use this in the future to fine-tune the ObjectInfo broker to provide "slices" of the meta-data when performance is an issue.

Usage (from our unit tests): 

            TestClass testClass = new TestClass() { Name = "Joe The Tester" };
            IObjectInfoBroker objectInfoBroker = new ObjectInfoBroker();

- Get ObjectInfo object

            ObjInfo expectedObjectInfo = 
            ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, testClass);

- Navigate Implemented Interfaces

            string? expectedImplementedInterfaceName =
                expectedObjectInfo!.TypeInfo!.ImplementedInterfaces!.
                FirstOrDefault(a => a.Name.Equals("ITestClass")).Name;

- Navigate MethodInfo

            string? expectedMethodInfo =
                expectedObjectInfo!.TypeInfo!.MethodInfos!.
                FirstOrDefault(a => a.Name.Equals("EnsureCompliance")).Name;

- Navigate PropertyInfo

            string? expectedPropInfo =
                expectedObjectInfo!.TypeInfo!.PropInfos!.
                FirstOrDefault(a => a.Name.Equals("Name")).Name;

- Navigate Type AttributeInfo

            string? expectedAttrInfo =
                expectedObjectInfo!.TypeInfo!.CustomAttrs!.
                FirstOrDefault(a => a.Name.Equals("IsCompliant")).Name;

- Navigate Method AttributeInfo

            var expectedMethInfo =
                expectedObjectInfo!.TypeInfo!.MethodInfos!.
                FirstOrDefault(a => a.Name.Equals("EnsureCompliance"));

            string? expectedAttrInfo =
                expectedMethInfo.CustomAttrs!.
                FirstOrDefault(a => a.Name.Equals("IsCompliant")).Name;

- Navigate Property AttributeInfo

            var expectedPropInfo =
                expectedObjectInfo!.TypeInfo!.PropInfos!.
                FirstOrDefault(a => a.Name.Equals("Name"));

            string? expectedAttrInfo =
                expectedPropInfo.CustomAttrs!.
                FirstOrDefault(a => a.Name.Equals("IsCompliant")).Name;
