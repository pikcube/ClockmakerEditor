using System;
using System.Collections.Generic;
using Avalonia.Input;

namespace Clockmaker0.Data;

/// <summary>
/// Object for transferring an in memory objects with a drag and drop.
/// </summary>
/// <param name="value">The reference to transfer</param>
/// <param name="text">A fallback text representation</param>
/// <typeparam name="T">The type of the object to transfer</typeparam>
public class CustomDataTransfer<T>(T value, string text) : IDataTransfer
{
    /// <summary>
    /// The value being transferred
    /// </summary>
    public T Value => DataValue.Value;
    private CustomDataTransferItem<T> DataValue { get; } = new(value);

    private DataTransferItem TextItem { get; } = DataTransferItem.CreateText(text);


    // Required for IDataTransfer
    /// <summary>
    /// The formats being transferred
    /// </summary>
    public IReadOnlyList<DataFormat> Formats => [CustomDataTransferItem<T>.CustomFormat, .. TextItem.Formats];

    /// <inheritdoc />
    public IReadOnlyList<IDataTransferItem> Items => [DataValue, TextItem];

    /// <summary>
    /// Empty dispose method. There's no actual cleanup involved with this transfer
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}