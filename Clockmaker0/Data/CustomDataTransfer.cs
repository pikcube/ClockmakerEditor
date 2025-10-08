using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;

namespace Clockmaker0.Data;

public class CustomDataTransfer<T>(T value, string text) : IDataTransfer
{
    public T Value => DataValue.Value;
    private CustomDataTransferItem<T> DataValue { get; } = new(value);

    private DataTransferItem TextItem { get; } = DataTransferItem.CreateText(text);


    // Required for IDataTransfer
    public IReadOnlyList<DataFormat> Formats => [CustomDataTransferItem<T>.CustomFormat, .. TextItem.Formats];

    public IReadOnlyList<IDataTransferItem> Items => [DataValue, TextItem];

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}