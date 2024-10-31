using System;

namespace Asv.IO
{
    public abstract class SpanKeyWithNameAndDescriptionType<TKey> : SpanKeyWithNameType<TKey>
    {
        private string _description;

        protected abstract void InternalValidateDescription(string description);

        public override void Deserialize(ref ReadOnlySpan<byte> buffer)
        {
            base.Deserialize(ref buffer);
            Description = BinSerialize.ReadString(ref buffer);
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                InternalValidateDescription(_description);
            }
        }

        public override void Serialize(ref Span<byte> buffer)
        {
            base.Serialize(ref buffer);
            BinSerialize.WriteString(ref buffer, _description);
        }

        public override int GetByteSize()
        {
            return base.GetByteSize() + BinSerialize.GetSizeForString(_description);
        }

        public override string ToString()
        {
            return $"[{Id}] {Name} ({Description})";
        }
    }
}
