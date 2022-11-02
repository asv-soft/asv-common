using System;
using System.Collections.Generic;
using LiteDB;

namespace Asv.Store
{
    public class TextMessageQueryResult
    {
        public int All { get; }
        public int Filtered { get; }
        public IEnumerable<TextMessage> Items { get; }
    }

    public class TextMessage
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public DateTime Date { get; set; }
        public int IntTag { get; set; }
        public string StrTag { get; set; }
        public string Text { get; set; }
    }

    public class TextMessageQuery
    {
        public int Take { get; set; } = 50;
        public int Skip { get; set; } = 0;
        public DateTime? Begin { get; set; }
        public DateTime? End { get; set; }
        public int[] IntTags { get; set; }
        public string[] StrTags { get; set; }
        public string Search { get; set; }
        public bool OrderAscending { get; set; }
    }

    public interface ITextStore
    {
        IEnumerable<TextMessage> Find(TextMessageQuery query);
        int Count(TextMessageQuery query);
        int Count();
        void Insert(TextMessage textMessage);
        void ClearAll();
    }
}
