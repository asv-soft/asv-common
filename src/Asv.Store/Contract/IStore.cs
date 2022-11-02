using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Asv.Store
{
    

    public interface IStore:IDisposable
    {
        string SourceName { get; }

        IEnumerable<string> Dicts { get; }
        IKeyValueStore GetDict(string name);

        IEnumerable<string> Texts { get; }
        ITextStore GetText(string name);

        IEnumerable<string> DoubleSeries { get; }
        ISeriesValueStore<double,TYValue> GetDoubleSeries<TYValue>(string name);

        IEnumerable<string> IntSeries { get; }
        ISeriesValueStore<int, TYValue> GetIntSeries<TYValue>(string name);

        IEnumerable<string> FileGrids { get; }
        IFileGrid GetFileGrid(string name);

        IEnumerable<string> RecordSeries { get; }
        
        ISimpleSeries<TRecord> GetRecordSeries<TRecord, TKey>(string name, Expression<Func<TRecord, TKey>> keyMapper);

        IEnumerable<string> DynamicTables { get; }
        IDynamicTablesStore GetDynamicTables(string name);
    }
}
