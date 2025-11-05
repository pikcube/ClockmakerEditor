using System;
using System.Diagnostics.CodeAnalysis;

namespace Clockmaker0.Data;

/// <summary>
/// Generic argument class for storing a value of a generic type
/// </summary>

public class SimpleEventArgs<T> : EventArgs
{
    /// <inheritdoc />
    public SimpleEventArgs()
    {
    }

    /// <inheritdoc />
    [SetsRequiredMembers]
    public SimpleEventArgs(T value)
    {
        Value = value;
    }

    /// <summary>
    /// The value
    /// </summary>
    public required T Value { get; init; }
}

/// <summary>
/// Generic argument class for storing two values of generic types
/// </summary>
public class SimpleEventArgs<T1, T2> : EventArgs
{
    /// <summary>
    /// The first value
    /// </summary>
    public required T1 Value1 { get; init; }
    /// <summary>
    /// The second value
    /// </summary>
    public required T2 Value2 { get; init; }

    /// <inheritdoc />
    public SimpleEventArgs()
    {
    }

    /// <inheritdoc />
    [SetsRequiredMembers]
    public SimpleEventArgs(T1 value1, T2 value2)
    {
        Value1 = value1;
        Value2 = value2;
    }
}