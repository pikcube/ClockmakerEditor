using Avalonia.Input;
using System.Collections.Generic;

namespace Clockmaker0.Data;

/// <summary>
/// The single item in a CustomDataTransfer. Holds a reference to an in memory object
/// </summary>
/// <param name="value">The value to be transferred</param>
/// <typeparam name="T">The type of the object being transferred</typeparam>
public class CustomDataTransferItem<T>(T value) : IDataTransferItem
{
    /// <summary>
    /// The value being transferred
    /// </summary>
    public T Value { get; } = value;


    //Required To Implement IDataTransferItem Interface

    /// <summary>
    /// Do not use this method, use Value to access the object without boxing.
    /// </summary>
    /// <param name="format">If this is not the single format in CustomFormat, this will return null</param>
    /// <returns></returns>
    public object? TryGetRaw(DataFormat format) => format == CustomFormat ? Value : null;

    /// <summary>
    /// A single readonly list containing CustomFormat
    /// </summary>
    public IReadOnlyList<DataFormat> Formats => [CustomFormat];

    /// <summary>
    /// The DataFormat of this object. Unnecesssary unless you are attempting to access this via the interface.
    /// </summary>
    public static DataFormat CustomFormat { get; } = DataFormat.CreateBytesApplicationFormat($"ClockmakerInternal{typeof(T)}PassingFormat-DoesNotReturnByteArray");
}