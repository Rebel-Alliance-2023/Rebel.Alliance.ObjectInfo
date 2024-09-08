# ObjectInfo Test Suite Documentation

This document provides a detailed explanation of the test cases in the `ObjectInfoBrokerTest.cs` file for the ObjectInfo library.

## Overview

The test suite uses xUnit as the testing framework and FluentAssertions for more expressive assertions. It focuses on testing the `ObjectInfoBroker` class and its ability to retrieve and navigate object information.

## Test Cases

### 1. ShouldRetrieveObjectInfo

**Purpose:** Verifies that the `ObjectInfoBroker` can correctly retrieve object information and compare it.

**Steps:**
1. Creates three `TestClass` instances with different names.
2. Retrieves `ObjInfo` for each instance using `ObjectInfoBroker`.
3. Compares the `ObjInfo` of the first two instances (which should be equivalent).
4. Compares the `ObjInfo` of the first and third instances (which should not be equivalent).

**Assertions:**
- The `ObjInfo` of the first two instances should be equivalent.
- The `ObjInfo` of the first and third instances should not be equivalent.

### 2. ShouldNavigateInterfaceInfo

**Purpose:** Ensures that the `ObjectInfoBroker` correctly retrieves information about implemented interfaces.

**Steps:**
1. Creates a `TestClass` instance.
2. Retrieves `ObjInfo` for the instance.
3. Attempts to find the implemented interface named "ITestClass" in the `ImplementedInterfaces` collection.

**Assertions:**
- The implemented interface should not be null.
- The interface name should be "ITestClass".

### 3. ShouldNavigateMethodInfo

**Purpose:** Verifies that the `ObjectInfoBroker` correctly retrieves information about methods.

**Steps:**
1. Creates a `TestClass` instance.
2. Retrieves `ObjInfo` for the instance.
3. Attempts to find the method named "EnsureCompliance" in the `MethodInfos` collection.

**Assertions:**
- The method info should not be null.
- The method name should be "EnsureCompliance".

### 4. ShouldNavigatePropInfo

**Purpose:** Ensures that the `ObjectInfoBroker` correctly retrieves information about properties.

**Steps:**
1. Creates a `TestClass` instance.
2. Retrieves `ObjInfo` for the instance.
3. Attempts to find the property named "Name" in the `PropInfos` collection.

**Assertions:**
- The property info should not be null.
- The property name should be "Name".

### 5. ShouldNavigateTypeAttributeInfo

**Purpose:** Verifies that the `ObjectInfoBroker` correctly retrieves information about type-level attributes.

**Steps:**
1. Creates a `TestClass` instance.
2. Retrieves `ObjInfo` for the instance.
3. Attempts to find the attribute named "IsCompliant" in the `CustomAttrs` collection of the type info.

**Assertions:**
- The attribute info should not be null.
- The attribute name should be "IsCompliant".

### 6. ShouldNavigatePropertyAttributeInfo

**Purpose:** Ensures that the `ObjectInfoBroker` correctly retrieves information about property-level attributes.

**Steps:**
1. Creates a `TestClass` instance.
2. Retrieves `ObjInfo` for the instance.
3. Finds the "Name" property in the `PropInfos` collection.
4. Attempts to find the attribute named "IsCompliant" in the `CustomAttrs` collection of the property.

**Assertions:**
- The attribute info should not be null.
- The attribute name should be "IsCompliant".

### 7. ShouldNavigateMethodAttributeInfo

**Purpose:** Verifies that the `ObjectInfoBroker` correctly retrieves information about method-level attributes.

**Steps:**
1. Creates a `TestClass` instance.
2. Retrieves `ObjInfo` for the instance.
3. Finds the "EnsureCompliance" method in the `MethodInfos` collection.
4. Attempts to find the attribute named "IsCompliant" in the `CustomAttrs` collection of the method.

**Assertions:**
- The attribute info should not be null.
- The attribute name should be "IsCompliant".

## Test Class Structure

The tests use a `TestClass` defined in `Test.ObjectInfoService.cs` with the following structure:

- Implements `ITestClass` interface
- Has a `Name` property
- Has an `EnsureCompliance` method
- Is decorated with the `IsCompliant` attribute at the class, property, and method levels

This structure allows for comprehensive testing of the `ObjectInfoBroker`'s ability to navigate and retrieve various types of metadata about a class.

