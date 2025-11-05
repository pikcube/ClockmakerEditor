using System.Diagnostics.CodeAnalysis;
using Pikcube.ReadWriteScript.Core;

namespace Clockmaker0.Data;

/// <summary>
/// A box for SortType enums with a custom ToString method
/// </summary>
/// <param name="sortType">The value to box</param>
public class SortEnumBox(SortType sortType)
{
    /// <summary>
    /// The boxxed value
    /// </summary>
    public SortType Value { get; init; } = sortType;

    /// <summary>
    /// Unbox an enum back to its base
    /// </summary>
    /// <param name="box">The value to unbox</param>
    /// <returns>The member of Value</returns>
    public static implicit operator SortType(SortEnumBox box) => box.Value;



    /// <summary>
    /// Box an enum with a custom ToString method
    /// </summary>
    /// <param name="sortType">The enum to box</param>
    /// <returns>The boxxed enum</returns>
    public static implicit operator SortEnumBox(SortType sortType) => new(sortType);

    /// <summary>
    /// Check if two Sort Enum Boxes by value
    /// </summary>
    /// <param name="value1">Left hand side</param>
    /// <param name="value2">Right hand side</param>
    /// <returns>True if they are equal, false otherwise</returns>
    public static bool operator ==(SortEnumBox value1, SortEnumBox value2) => value1.Value == value2.Value;
    /// <summary>
    /// Check if two Sort Enum Boxes by value
    /// </summary>
    /// <param name="value1">Left hand side</param>
    /// <param name="value2">Right hand side</param>
    /// <returns>False if they are equal, true otherwise</returns>
    public static bool operator !=(SortEnumBox value1, SortEnumBox value2) => !(value1 == value2);

    /// <inheritdoc />
    public override string ToString() => Extensions.NormalizedSortStrings[Value];

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj switch
        {
            SortType st => st == Value,
            SortEnumBox seb => seb == this,
            _ => false
        };
    }

    /// <inheritdoc />
    public override int GetHashCode() => (int)Value;
}