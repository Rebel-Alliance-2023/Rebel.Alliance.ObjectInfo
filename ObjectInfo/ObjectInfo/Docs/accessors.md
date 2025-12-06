# Accessors: Fast Property Getter/Setter Delegates

## Executive Summary

`Accessors` provides near-zero overhead, open-instance delegates for property getters and setters. It uses `Delegate.CreateDelegate` when the target is a property method and falls back to expression compilation otherwise. This removes repeated expression compilation and reflection costs in hot paths (rendering, editing), improving responsiveness with minimal complexity.

## Problem Addressed

### Before Optimization

Creating delegates from expressions often uses `LambdaExpression.Compile()`, which:
- Allocates dynamic methods and closures
- Adds JIT work and runtime cost per compilation
- Is unnecessary when the expression targets a simple property access

In data-bound components and batch processing, these delegates may be created frequently, amplifying overhead.

## Accessors Solution

### API Surface

```csharp
public static class Accessors
{
    public static Func<TTarget, TProp> CreateGetter<TTarget, TProp>(Expression<Func<TTarget, TProp>> expr);
    public static Action<TTarget, TProp>? CreateSetter<TTarget, TProp>(Expression<Func<TTarget, TProp>> expr);
}
```

### How It Works

- Detects `MemberExpression` targeting a `PropertyInfo`
- If the property has a getter or setter:
  - Creates an open-instance delegate via `Delegate.CreateDelegate`
  - This avoids expression compilation and uses the underlying `MethodInfo` directly
- If the expression is not a property (fields, indexers, computed members):
  - Getter falls back to `expr.Compile()`
  - Setter returns `null` (no generic field/setter support)

### Key Characteristics

- Near-zero overhead for property accessors
- No allocations on the fast path (aside from the delegate itself)
- Works for any `TTarget`/`TProp` with property access
- Safe fallback to compiled expressions when needed

## Code Walkthrough

### Getter Path

```csharp
if (expr.Body is MemberExpression me && me.Member is PropertyInfo pi && pi.GetMethod is MethodInfo get)
{
    return (Func<TTarget, TProp>)Delegate.CreateDelegate(typeof(Func<TTarget, TProp>), get);
}
return expr.Compile();
```

- If `expr` is `x => x.Property`, create `Func<TTarget, TProp>` bound to the `get_Property` method
- Otherwise compile the lambda (supports fields, indexers, computed members)

### Setter Path

```csharp
if (expr.Body is MemberExpression me && me.Member is PropertyInfo pi && pi.SetMethod is MethodInfo set)
{
    return (Action<TTarget, TProp>)Delegate.CreateDelegate(typeof(Action<TTarget, TProp>), set);
}
return null;
```

- Only properties with a `set` method are supported
- Non-property expressions (fields, indexers) return `null`

## Integration in Editable Scenarios

- Use `CreateGetter` to read values for display without incurring expression compilation in hot paths
- Use `CreateSetter` to write edited values when available; fall back to model binding or manual assignment if `null`
- Cache created delegates per column to reuse across rows/renders

Example:

```csharp
// Setup per column
var get = Accessors.CreateGetter<Row, TValue>(x => x.SomeProperty);
var set = Accessors.CreateSetter<Row, TValue>(x => x.SomeProperty);

// Use during render/edit
var current = get(row);
if (set is not null) set(row, newValue);
```

## Performance Impact

### Micro-Level

- `Delegate.CreateDelegate` on a property method is fast, avoids `Compile()`
- Typical gains: multiple microseconds saved per avoided compilation and reduced GC pressure

### Macro-Level

- In a grid, caching these delegates per column removes repeated compilation costs during scrolling and editing
- Contributes to smoother frame times alongside `TypeTraits`

## Edge Cases and Behavior

- Expression must be a simple member access to a property: `x => x.Prop`
- Indexers, fields, or computed expressions compile for getters and have no setter support
- Works with nullable and non-nullable property types
- Returns `null` for read-only or non-property setter paths

## When to Use Accessors

Use `Accessors` when:
- You need fast property access in rendering or editing loops
- You can cache delegates per column/component instance
- You want to avoid expression compilation overhead

Avoid or fall back when:
- You target fields or indexers (getter compiles, setter unsupported)
- One-off delegate compilation is sufficient and clarity is preferred

## Best Practices

- Cache delegates once per column/component instance
- Validate `set` for `null` before using
- Prefer straightforward property access expressions for maximum benefit

## Summary

`Accessors` is a focused optimization for ObjectInfo scenarios. It replaces expensive expression compilation with direct method delegates for properties, reducing overhead in hot paths with minimal code complexity. Combine with `TypeTraits` to eliminate reflection and type-inspection costs for robust, responsive workflows.
