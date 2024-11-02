using System;
using Rebel.Alliance.ObjectInfo.Overlord.Markers;

namespace Overlord.Test.Library
{
    // Custom attribute for testing attribute scanning
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class CustomAttribute : Attribute
    {
        public string Description { get; }
        public CustomAttribute(string description)
        {
            Description = description;
        }
    }

    // Base class with MetadataScan attribute
    [MetadataScan(Description = "Base model for testing inheritance scanning")]
    public class BaseModel
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }

        public BaseModel()
        {
            CreatedAt = DateTime.Now;
        }

        public virtual void DisplayInfo()
        {
            Console.WriteLine($"ID: {Id}, Created At: {CreatedAt}");
        }
    }

    // Derived class implementing IMetadataScanned
    [Custom("This is a derived model class.")]
    public class DerivedModel : BaseModel, IMetadataScanned
    {
        public string Name { get; set; }
        public string Description { get; set; }

        [Custom("Override method for testing method metadata")]
        public override void DisplayInfo()
        {
            base.DisplayInfo();
            Console.WriteLine($"Name: {Name}, Description: {Description}");
        }

        public void UpdateDescription(string newDescription)
        {
            Description = newDescription;
        }
    }

    // Class for testing field and property scanning
    [MetadataScan(Description = "Model for testing field and property scanning")]
    public class AnotherModel : IMetadataScanned
    {
        private int _privateField;
        private readonly string _readOnlyField = "Read Only";
        public int PublicProperty { get; set; }
        public string ReadOnlyProperty { get; }
        public string WriteOnlyProperty { private get; set; }

        public AnotherModel(int initialValue)
        {
            _privateField = initialValue;
            ReadOnlyProperty = "Initial Value";
        }

        [Custom("Getter method")]
        public int GetPrivateField() => _privateField;

        [Custom("Setter method")]
        public void SetPrivateField(int value) => _privateField = value;
    }

    // Class for testing nested type scanning
    [MetadataScan(Description = "Container class for testing nested type scanning")]
    public class ContainerModel : IMetadataScanned
    {
        public string ContainerName { get; set; }

        [MetadataScan(Description = "Nested class for testing nested type scanning")]
        public class NestedModel : IMetadataScanned
        {
            public string NestedName { get; set; }
        }
    }

    // Generic class without constraints
    [MetadataScan(Description = "Generic model for testing type parameter scanning")]
    public class GenericModel<T> : IMetadataScanned
    {
        public T Data { get; set; }

        public GenericModel(T data)
        {
            Data = data;
        }

        public void DisplayData()
        {
            Console.WriteLine($"Data: {Data}");
        }
    }

    // Generic class with constraints
    [MetadataScan(Description = "Generic model with constraints for testing advanced type scanning")]
    public class GenericModelWithConstraints<T> : IMetadataScanned where T : ContainerModel
    {
        public T Data { get; set; }
        public string MetadataTag { get; set; }

        public GenericModelWithConstraints(T data)
        {
            Data = data;
            MetadataTag = typeof(T).GetCustomAttributes(typeof(MetadataScanAttribute), true)
                           .FirstOrDefault()?.ToString() ?? "No metadata";
        }

        public void DisplayData()
        {
            Console.WriteLine($"Data: {Data}, Metadata: {MetadataTag}");
        }
    }

    // Interface for testing interface scanning
    [MetadataScan(Description = "Interface for testing metadata scanning")]
    public interface ITestInterface : IMetadataScanned
    {
        string TestProperty { get; set; }
        void TestMethod();
    }

    // Implementation of test interface
    [MetadataScan(Description = "Implementation of test interface")]
    public class TestInterfaceImplementation : ITestInterface
    {
        public string TestProperty { get; set; }

        [Custom("Interface method implementation")]
        public void TestMethod()
        {
            Console.WriteLine("Test method implementation");
        }
    }

    // Static class for testing static member scanning
    [MetadataScan(Description = "Static class for testing static member scanning")]
    public static class StaticTestModel
    {
        public static int StaticProperty { get; set; }
        private static string StaticField = "Static Field";

        public static void StaticMethod()
        {
            Console.WriteLine("Static method called");
        }
    }

    // Abstract class for testing abstract member scanning
    [MetadataScan(Description = "Abstract class for testing abstract member scanning")]
    public abstract class AbstractTestModel : IMetadataScanned
    {
        public abstract string AbstractProperty { get; set; }
        public virtual string VirtualProperty { get; set; }

        public abstract void AbstractMethod();

        public virtual void VirtualMethod()
        {
            Console.WriteLine("Virtual method implementation");
        }
    }

    // Concrete implementation of abstract class
    [MetadataScan(Description = "Concrete implementation of abstract class")]
    public class ConcreteTestModel : AbstractTestModel
    {
        public override string AbstractProperty { get; set; }
        public override string VirtualProperty { get; set; }

        public override void AbstractMethod()
        {
            Console.WriteLine("Abstract method implementation");
        }

        public override void VirtualMethod()
        {
            base.VirtualMethod();
            Console.WriteLine("Overridden virtual method");
        }
    }
}
