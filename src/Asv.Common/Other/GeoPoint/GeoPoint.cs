using System;

namespace Asv.Common
{
    /// <summary>
    /// WGS 84 (EPSG:4326)
    /// https://en.wikipedia.org/wiki/World_Geodetic_System
    /// </summary>

    public struct GeoPoint : IEquatable<GeoPoint>
    {
        // West-East
        public readonly double Longitude;
        // Nord-South
        public readonly double Latitude;
        public readonly double Altitude;

        public static GeoPoint NaN => new(Double.NaN, Double.NaN, Double.NaN);
        public static GeoPoint Zero => new(0.0, 0.0,0.0);
        public static GeoPoint ZeroWithAlt => new(0.0, 0.0, 0.0);
        
        public GeoPoint(double latitude, double longitude, double altitude)
        {
            this.Latitude = latitude;
            this.Longitude = longitude;
            this.Altitude = altitude;
        }

        public static GeoPoint operator +(GeoPoint x, GeoPoint y)
        {
            return new GeoPoint(x.Latitude + y.Latitude, x.Longitude + y.Longitude, x.Altitude + y.Altitude);
        }

        public static GeoPoint operator -(GeoPoint x, GeoPoint y)
        {
            return new GeoPoint(x.Latitude - y.Latitude, x.Longitude - y.Longitude, x.Altitude - y.Altitude);
        }

        public override string ToString()
        {
            return $"Lat:{Latitude:F7},Lon:{Longitude:F7},Alt:{Altitude:F1} m";
        }

        public bool Equals(GeoPoint other)
        {
            return Longitude.Equals(other.Longitude) && Latitude.Equals(other.Latitude) && Nullable.Equals(Altitude, other.Altitude);
        }

        public override bool Equals(object obj)
        {
            return obj is GeoPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Longitude.GetHashCode() ^ Latitude.GetHashCode() ^ Altitude.GetHashCode();
        }
    }
    
}
