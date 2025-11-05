using System;
using System.Globalization;
using Asv.Common;
using R3;

namespace Asv.IO;

public static class ReactivePropertyPackagePartMixin
{
    public static ReactiveProperty<GeoPoint> AddGeoPoint(
        this ReactivePropertyPackagePart part,
        string key,
        GeoPoint defaultValue = default
    )
    {
        return part.AddProperty(key, defaultValue, LoadGeoPoint, SaveGeoPoint);
    }

    public static string SaveGeoPoint(GeoPoint val)
    {
        return $"{nameof(GeoPoint)};{val.Latitude.ToString(CultureInfo.InvariantCulture)};{val.Longitude.ToString(CultureInfo.InvariantCulture)};{val.Altitude.ToString(CultureInfo.InvariantCulture)}";
    }

    public static GeoPoint LoadGeoPoint(string str)
    {
        var parts = str.Split(';');
        if (parts is not [nameof(GeoPoint), _, _, _])
        {
            throw new InvalidOperationException($"Invalid geo point format: {str}");
        }

        return new GeoPoint(
            double.Parse(parts[1], CultureInfo.InvariantCulture),
            double.Parse(parts[2], CultureInfo.InvariantCulture),
            double.Parse(parts[3], CultureInfo.InvariantCulture)
        );
    }

    #region Double

    public static ReactiveProperty<double> AddDouble(
        this ReactivePropertyPackagePart part,
        string key,
        double defaultValue
    )
    {
        return part.AddProperty(key, defaultValue, LoadDouble, SaveDouble);
    }

    private static string SaveDouble(double val)
    {
        return $"{nameof(Double)};{val.ToString(CultureInfo.InvariantCulture)}";
    }

    private static double LoadDouble(string str)
    {
        var parts = str.Split(';');
        if (parts is not [nameof(Double), _])
        {
            throw new InvalidOperationException($"Invalid double format: {str}");
        }
        return double.Parse(parts[1], CultureInfo.InvariantCulture);
    }

    #endregion

    #region Long

    public static ReactiveProperty<long> AddInt64(
        this ReactivePropertyPackagePart part,
        string key,
        long defaultValue
    )
    {
        return part.AddProperty(key, defaultValue, LoadInt64, SaveInt64);
    }

    private static string SaveInt64(long val)
    {
        return $"{nameof(Int64)};{val.ToString(CultureInfo.InvariantCulture)}";
    }

    private static long LoadInt64(string str)
    {
        var parts = str.Split(';');
        if (parts is not [nameof(Int64), _])
        {
            throw new InvalidOperationException($"Invalid Int64 format: {str}");
        }
        return long.Parse(parts[1], CultureInfo.InvariantCulture);
    }

    #endregion

    #region Int32

    public static ReactiveProperty<int> AddInt32(
        this ReactivePropertyPackagePart part,
        string key,
        int defaultValue
    )
    {
        return part.AddProperty(key, defaultValue, LoadInt32, SaveInt32);
    }

    private static string SaveInt32(int val)
    {
        return $"{nameof(Int32)};{val.ToString(CultureInfo.InvariantCulture)}";
    }

    private static int LoadInt32(string str)
    {
        var parts = str.Split(';');
        if (parts is not [nameof(Int32), _])
        {
            throw new InvalidOperationException($"Invalid Int32 format: {str}");
        }
        return int.Parse(parts[1], CultureInfo.InvariantCulture);
    }

    #endregion

    #region String

    public static ReactiveProperty<string> AddString(
        this ReactivePropertyPackagePart part,
        string key,
        string defaultValue
    )
    {
        return part.AddProperty<string>(key, defaultValue, x => x, x => x);
    }

    #endregion
}
