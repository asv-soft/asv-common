using System;
using System.Collections.Generic;
using DynamicData;
using LiteDB;

namespace Asv.Store
{

    public interface IDynamicTableInfo
    {
        Guid TableId { get; }
    }

    public interface IColumnStatistic
    {
        int Count { get; }
    }

    public interface IDynamicTableStatistic
    {
        int RowCount { get; }
        int ColumnCount { get; }
    }

    public interface IDynamicTablesStore
    {
        string Name { get; }
        IEnumerable<IDynamicTableInfo> Tables { get; }

        IColumnStatistic GetColumnStatistic(Guid tableId, string columnName);
        bool TryReadCell(Guid tableId, string columnName, int rowIndex, out BsonValue value);
        void UpsetCell(Guid tableId, string columnName, int rowIndex, BsonValue value);

        IDynamicTableStatistic GetTableStatistic(Guid tableId);
        int ObserveAll(Guid tableId, Action<IDynamicTableRawObserver> callback);
        void AddRaw(Guid tableId, Action<IDynamicTableRawObserver> callback);
        bool RemoveRaw(Guid tableId, int rawIndex);
    }

    public static class DynamicTablesStoreHelper
    {
        public static bool TryReadDoubleCell(this IDynamicTablesStore src, Guid tableId, string columnName, int rowIndex, out double value)
        {
            if (src.TryReadCell(tableId, columnName, rowIndex, out var bsonValue) && bsonValue.IsDouble)
            {
                value = bsonValue.AsDouble;
                return true;
            }
            value = double.NaN;
            return false;
        }

        public static bool TryReadEnumCell<T>(this IDynamicTablesStore src, Guid tableId, string columnName, int rowIndex, out T value)
            where T : struct, Enum
        {
            if (src.TryReadCell(tableId, columnName, rowIndex, out var bsonValue) && bsonValue.IsString)
            {
                if (Enum.TryParse(bsonValue.AsString, out value))
                {
                    return true;
                }
            }
            value = default;
            return false;
        }

        public static void UpsetEnumCell<T>(this IDynamicTablesStore src, Guid tableId, string groupName, string columnName, int rowIndex, T value)
            where T : Enum
        {
            src.UpsetCell(tableId, columnName, rowIndex, value.ToString());
        }
        public static bool TryReadDoubleCell(this IDynamicTableRawObserver src, string columnName, out double value)
        {
            if (src.TryReadCell(columnName, out var bsonValue) && bsonValue.IsDouble)
            {
                value = bsonValue.AsDouble;
                return true;
            }
            value = double.NaN;
            return false;
        }

        public static bool TryReadEnumCell<T>(this IDynamicTableRawObserver src, string columnName, out T value)
            where T : struct, Enum
        {
            if (src.TryReadCell(columnName, out var bsonValue) && bsonValue.IsString)
            {
                if (Enum.TryParse(bsonValue.AsString, out value))
                {
                    return true;
                }
            }
            value = default;
            return false;
        }

        public static void WriteEnumCell<T>(this IDynamicTableRawObserver src, string groupName, string columnName, T value)
            where T : Enum
        {
            src.WriteCell(columnName, value.ToString());
        }
    }

    public interface IDynamicTableRawObserver
    {
        Guid TableId { get; }
        int RawIndex { get; }
        IEnumerable<string> Columns { get; }
        bool TryReadCell(string columnName, out BsonValue value);
        void WriteCell(string columnName, BsonValue value);
    }

}
