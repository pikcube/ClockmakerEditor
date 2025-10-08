using System.Diagnostics.CodeAnalysis;
using Pikcube.ReadWriteScript.Core;

namespace Clockmaker0.Data;

public class SortEnumBox(SortType sortType)
{
    public SortType Value { get; init; } = sortType;

    public static implicit operator SortType(SortEnumBox box) => box.Value;
    public static implicit operator SortEnumBox(SortType sortType) => new(sortType);

    public static bool operator ==(SortEnumBox value1, SortEnumBox value2) => value1.Value == value2.Value;
    public static bool operator !=(SortEnumBox value1, SortEnumBox value2) => value1.Value != value2.Value;

    public override string ToString() => Extensions.NormalizedSortStrings[Value];

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj switch
        {
            SortType st => st == Value,
            SortEnumBox seb => seb == this,
            _ => false
        };
    }

    protected bool Equals(SortEnumBox other) => Value == other.Value;

    public override int GetHashCode() => (int)Value;
}