using System;

namespace Asv.IO
{
    public abstract class SpanKeyWithNameType<TKey> : ISizedSpanSerializable
    {
        private string _name;

        protected abstract void InternalValidateName(string name);
        protected abstract TKey InternalReadKey(ref ReadOnlySpan<byte> buffer);
        protected abstract void InternalWriteKey(ref Span<byte> buffer, TKey id);
        protected abstract int InternalGetSizeKey(TKey id);

        public TKey Id { get; set; }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                InternalValidateName(_name);
            }
        }

        public virtual void Deserialize(ref ReadOnlySpan<byte> buffer)
        {
            Id = InternalReadKey(ref buffer);
            _name = BinSerialize.ReadString(ref buffer);
        }

        public virtual void Serialize(ref Span<byte> buffer)
        {
            InternalWriteKey(ref buffer, Id);
            BinSerialize.WriteString(ref buffer, _name);
        }

        public virtual int GetByteSize()
        {
            return BinSerialize.GetSizeForString(_name) + InternalGetSizeKey(Id);
        }

        public override string ToString()
        {
            return $"[{Id}] {Name}";
        }
    }
}
