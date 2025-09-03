using System;

namespace Asv.Common
{
    /// <summary>
    /// WGS 84 (EPSG:4326)
    /// https://en.wikipedia.org/wiki/World_Geodetic_System
    /// </summary>

    public readonly struct GeoPoint(double latitude, double longitude, double altitude) : IEquatable<GeoPoint>
    {
        public const char Delimiter = ';';

        public static GeoPoint Parse(string value)
        {
            if (TryParse(value, out var geoPoint) == false)
            {
                throw new ArgumentException($"Cannot parse '{value}' to GeoPoint", nameof(value));
            }
            return geoPoint;
        }
        
        public static bool TryParse(string value, out GeoPoint geoPoint)
        {
            var arr = value.Split(Delimiter);
            switch (arr.Length)
            {
                case 3:
                    return TryParse(arr[0], arr[1], arr[2],out geoPoint);
                case 2:
                    return TryParse(arr[0], arr[1], out geoPoint);
                default:
                    geoPoint = default;
                    return false;
            }
        }
        
        public static bool TryParse(string latitude, string longitude, out GeoPoint geoPoint)
        {
            if (GeoPointLatitude.TryParse(latitude, out var lat) == false || GeoPointLongitude.TryParse(longitude, out var lon) == false)
            {
                geoPoint = default;
                return false;
            }

            geoPoint = new GeoPoint(lat, lon, 0);
            return true;
        }
        
        public static bool TryParse(string latitude, string longitude, string altitude, out GeoPoint geoPoint)
        {
            if (GeoPointLatitude.TryParse(latitude, out var lat) == false || GeoPointLongitude.TryParse(longitude, out var lon) == false)
            {
                geoPoint = default;
                return false;
            }

            double alt;
            var result = InvariantNumberParser.TryParse(altitude, out alt);
            if (result.IsSuccess == false)
            {
                geoPoint = default;
                return false;
            }

            geoPoint = new GeoPoint(lat, lon, alt);
            return true;
        }
        public static GeoPoint Random(Random? random = null, double minLatitude = -90.0, double maxLatitude = 90.0, double minLongitude = -180.0, double maxLongitude = 180.0, double minAltitude = -10000.0, double maxAltitude = 10000.0)
        {
            random ??= System.Random.Shared;
            var latitude = random.NextDouble() * (maxLatitude - minLatitude) + minLatitude;
            var longitude = random.NextDouble() * (maxLongitude - minLongitude) + minLongitude;
            var altitude = random.NextDouble() * (maxAltitude - minAltitude) + minAltitude;
            return new GeoPoint(latitude, longitude, altitude);
        }
        
        // West-East
        public readonly double Longitude = longitude;
        // Nord-South
        public readonly double Latitude = latitude;
        public readonly double Altitude = altitude;

        public static GeoPoint NaN => new(Double.NaN, Double.NaN, Double.NaN);
        public static GeoPoint Zero => new(0.0, 0.0,0.0);
        public static GeoPoint ZeroWithAlt => new(0.0, 0.0, 0.0);

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
            return $"{GeoPointLatitude.PrintDms(Latitude)}{Delimiter}{GeoPointLongitude.PrintDms(Longitude)}{Delimiter}{Altitude}";
        }
        
        /// <summary>
        /// Determines whether this geographic point is equal to another one within a fixed tolerance.
        /// </summary>
        /// <param name="other">The other geographic point to compare with.</param>
        /// <returns>
        /// <c>true</c> if the longitude, latitude, and altitude approximately equals, otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(GeoPoint other)
        {
            return Longitude.ApproximatelyEquals(other.Longitude) 
                   && Latitude.ApproximatelyEquals(other.Latitude) 
                   && Altitude.ApproximatelyEquals(other.Altitude); 
        }
        
        /// <summary>
        /// Determines whether this geographic point is equal to another one within a specified tolerance.
        /// </summary>
        /// <param name="other">The other geographic point to compare with.</param>
        /// <param name="epsilon">The allowed tolerance when comparing coordinates.</param>
        /// <returns>
        /// <c>true</c> if the longitude, latitude, and altitude differ by no more than the specified <paramref name="epsilon"/>; 
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(GeoPoint other, double epsilon)
        {
            return Longitude.ApproximatelyEquals(other.Longitude, epsilon) 
                   && Latitude.ApproximatelyEquals(other.Latitude, epsilon) 
                   && Altitude.ApproximatelyEquals(other.Altitude, epsilon); 
        }
        
        public override bool Equals(object? obj)
        {
            return obj is GeoPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Longitude, Latitude, Altitude);
        }
        
        public static bool operator ==(GeoPoint left, GeoPoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GeoPoint left, GeoPoint right)
        {
            return !left.Equals(right);
        }
    }
    
}
