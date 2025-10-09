using Avalonia.Input;
using System.Collections.Generic;

namespace Clockmaker0.Data;

public class CustomDataTransferItem<T>(T value) : IDataTransferItem
{
    public T Value { get; } = value;


    //Required To Implement IDataTransferItem Interface

    public object? TryGetRaw(DataFormat format) => format == CustomFormat ? Value : null;

    public IReadOnlyList<DataFormat> Formats => [CustomFormat];

    public static DataFormat CustomFormat { get; } = DataFormat.CreateStringApplicationFormat($"ClockmakerInternal{typeof(T)}PassingFormat");
}