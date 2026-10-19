using System;

namespace Asv.Common
{
    public class CsvColumn<T>
    {
        public string Name { get; }
        private readonly Func<T, string> _getter;

        public CsvColumn(string name, Func<T, string> getter)
        {
            Name = name;
            _getter = getter;
        }

        public string Render(T val)
        {
            return _getter(val);
        }
    }
}
