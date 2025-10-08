using System;
using System.Diagnostics.CodeAnalysis;

namespace Clockmaker0.Data;

public class SimpleEventArgs<T> : EventArgs
{
    public SimpleEventArgs()
    {
    }
    [SetsRequiredMembers]
    public SimpleEventArgs(T value)
    {
        Value = value;
    }

    public required T Value { get; set; }
}

public class SimpleEventArgs<T1, T2> : EventArgs
{
    public required T1 Value1 { get; set; }
    public required T2 Value2 { get; set; }

    public SimpleEventArgs()
    {
    }
    [SetsRequiredMembers]
    public SimpleEventArgs(T1 value1, T2 value2)
    {
        Value1 = value1;
        Value2 = value2;
    }
}