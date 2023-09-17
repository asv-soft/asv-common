using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Asv.Cfg
{
    public static class ConfigurationHelper
    {
        private const string NameRegexString = @"^(?!\d)[\w$]+$";
        private static readonly Regex KeyRegex = new(NameRegexString, RegexOptions.Compiled);
        public static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));
            if (KeyRegex.IsMatch(key) == false) 
                throw new ArgumentException($"Invalid key '{key}': must be {NameRegexString}");
        }
        
        public static IEqualityComparer<string> DefaultKeyComparer { get; } = StringComparer.InvariantCultureIgnoreCase;

       
    }
    
    public interface IConfiguration:IDisposable
    {
        IEnumerable<string> AvailableParts { get; }
        [Obsolete("Use Exist(string key) instead")]
        bool Exist<TPocoType>(string key);
        bool Exist(string key);
        TPocoType Get<TPocoType>(string key, TPocoType defaultValue);
        void Set<TPocoType>(string key, TPocoType value);
        void Remove(string key);
    }

    public static class ConfigurationExtensions
    {
        public static TPocoType Get<TPocoType>(this IConfiguration src,string key) where TPocoType : new()
        {
            var defaultValue = new TPocoType();
            return src.Get(key, defaultValue);
        }

        public static void Update<TPocoType>(this IConfiguration src, Action<TPocoType> updateCallback) where TPocoType : new()
        {
            var value = src.Get<TPocoType>();
            updateCallback(value);
            src.Set(value);
        }

        public static TPocoType Get<TPocoType>(this IConfiguration src)
            where TPocoType :  new()
        {
            var defaultValue = new TPocoType();
            var value = src.Get(typeof(TPocoType).Name, defaultValue);
            return value;
        }

        public static void Set<TPocoType>(this IConfiguration src, TPocoType value)
            where TPocoType : new()
        {
            src.Set(typeof(TPocoType).Name, value);
        }

        public static void Remove<TPocoType>(this IConfiguration src)
            where TPocoType :  new()
        {
            src.Remove(typeof(TPocoType).Name);
        }

    }

    
}
