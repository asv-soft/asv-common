using System;
using System.Collections.Generic;
using Asv.Common;
using LiteDB;

namespace Asv.Store
{
    public class RxStoredGeoPoint : RxStoredValue<GeoPoint>
    {
        public RxStoredGeoPoint(IKeyValueStore store, string id, GeoPoint defaultValue, TimeSpan? saveDelay = null) : base(store, id, defaultValue, saveDelay)
        {
        }

        protected override GeoPoint ConvertFromBson(BsonValue bson)
        {
            if (bson.IsNull) return GeoPoint.ZeroWithAlt;
            var doc = bson.AsDocument;
            return new GeoPoint(doc["lat"].AsDouble, doc["lon"].AsDouble, doc["alt"].AsDouble);
        }

        protected override BsonValue ConvertToBson(GeoPoint value)
        {
            return new BsonDocument(new Dictionary<string, BsonValue>
            {
                { "lat", value.Latitude},
                { "lon", value.Longitude},
                { "alt", value.Altitude},
            });
        }
    }
}
