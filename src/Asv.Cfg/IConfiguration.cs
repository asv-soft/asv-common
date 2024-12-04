using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using R3;

namespace Asv.Cfg
{
    public static partial class ConfigurationHelper
    {
        private const string FixedNameRegexString = @"^(?!\d)[\w$]+$";
        [GeneratedRegex(FixedNameRegexString, RegexOptions.Compiled)]
        private static partial Regex MyRegex();
        private static readonly Regex KeyRegex = MyRegex();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidateKey(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            if (KeyRegex.IsMatch(key) == false)
                throw new ArgumentException($"Invalid key '{key}': must be {FixedNameRegexString}");
        }
        
        public static IEqualityComparer<string> DefaultKeyComparer { get; } = StringComparer.InvariantCultureIgnoreCase;
        
        [Obsolete("Use Get<TPocoType>(string key, Lazy<TPocoType> defaultValue)")]
        public static TPocoType Get<TPocoType>(this IConfiguration src, string key, TPocoType defaultValue)
        {
            return src.Get(key, new Lazy<TPocoType>(() => defaultValue));
        }
        public static TPocoType Get<TPocoType>(this IConfiguration src,string key) where TPocoType : new()
        {
            return src.Get(key, new Lazy<TPocoType>(() => new TPocoType()));
        }

        public static void Update<TPocoType>(this IConfiguration src,Action<TPocoType> updateCallback)
            where TPocoType : new()
        {
            var value = src.Get<TPocoType>();
            updateCallback(value);
            src.Set(value);
        }

        public static TPocoType Get<TPocoType>(this IConfiguration src)
            where TPocoType :  new()
        {
            return src.Get(typeof(TPocoType).Name, new Lazy<TPocoType>(() => new TPocoType()));
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
    
    public interface IConfiguration:IDisposable
    {
        IEnumerable<string> ReservedParts { get; }
        IEnumerable<string> AvailableParts { get; }
        bool Exist(string key);
        TPocoType Get<TPocoType>(string key, Lazy<TPocoType> defaultValue);
        void Set<TPocoType>(string key, TPocoType value);
        void Remove(string key);
        Observable<ConfigurationException> OnError { get; }
        Observable<KeyValuePair<string, object?>> OnChanged { get; }
    }

    /// <summary>
    /// If you want to save/load configuration from/to IConfiguration by custom way, you can implement this interface
    /// IConfiguration will call Load/Save methods when it needs to save/load configuration
    /// </summary>
    public interface ICustomConfigurable
    {
        public void Load(IConfiguration configuration);
        public void Save(IConfiguration configuration);
    }
}
