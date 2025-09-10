using System;
using System.Text;

namespace Asv.IO;

public class PrintValueVisitor(StringBuilder sb, bool skipUnknown) : FullVisitorBase(skipUnknown)
{
    private bool _first = true;

    private void CheckFirst()
    {
        if (_first)
        {
            _first = false;
        }
        else
        {
            sb.Append(", ");
        }
    }

    public override void Visit(Field field, UInt8Type type, ref byte value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public override void Visit(Field field, HalfFloatType type, ref Half value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public override void Visit(Field field, Int8Type type, ref sbyte value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public override void Visit(Field field, Int16Type type, ref short value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public override void Visit(Field field, UInt16Type type, ref ushort value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public override void Visit(Field field, Int32Type type, ref int value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public override void Visit(Field field, UInt32Type type, ref uint value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public override void Visit(Field field, Int64Type type, ref long value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public override void Visit(Field field, UInt64Type type, ref ulong value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public override void Visit(Field field, DoubleType type, ref double value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public override void Visit(Field field, FloatType type, ref float value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public override void Visit(Field field, StringType type, ref string value)
    {
        CheckFirst();
        sb.Append('\'');
        sb.Append(value);
        sb.Append('\'');
    }

    public override void Visit(Field field, BoolType type, ref bool value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public override void Visit(Field field, CharType type, ref char value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public override void Visit(Field field, DoubleOptionalType type, ref double? value)
    {
        CheckFirst();
        if (value.HasValue)
        {
            sb.Append(value.Value);
        }
        else
        {
            sb.Append("null");
        }
    }

    public override void Visit(Field field, FloatOptionalType type, ref float? value)
    {
        CheckFirst();
        if (value.HasValue)
        {
            sb.Append(value.Value);
        }
        else
        {
            sb.Append("null");
        }
    }

    public override void Visit(Field field, HalfFloatOptionalType type, ref Half? value)
    {
        CheckFirst();
        if (value.HasValue)
        {
            sb.Append(value.Value);
        }
        else
        {
            sb.Append("null");
        }
    }

    public override void Visit(Field field, Int8OptionalType type, ref sbyte? value)
    {
        CheckFirst();
        if (value.HasValue)
        {
            sb.Append(value.Value);
        }
        else
        {
            sb.Append("null");
        }
    }

    public override void Visit(Field field, Int16OptionalType type, ref short? value)
    {
        CheckFirst();
        if (value.HasValue)
        {
            sb.Append(value.Value);
        }
        else
        {
            sb.Append("null");
        }
    }

    public override void Visit(Field field, Int32OptionalType type, ref int? value)
    {
        CheckFirst();
        if (value.HasValue)
        {
            sb.Append(value.Value);
        }
        else
        {
            sb.Append("null");
        }
    }

    public override void Visit(Field field, Int64OptionalType type, ref long? value)
    {
        CheckFirst();
        if (value.HasValue)
        {
            sb.Append(value.Value);
        }
        else
        {
            sb.Append("null");
        }
    }

    public override void Visit(Field field, UInt8OptionalType type, ref byte? value)
    {
        CheckFirst();
        if (value.HasValue)
        {
            sb.Append(value.Value);
        }
        else
        {
            sb.Append("null");
        }
    }

    public override void Visit(Field field, UInt16OptionalType type, ref ushort? value)
    {
        CheckFirst();
        if (value.HasValue)
        {
            sb.Append(value.Value);
        }
        else
        {
            sb.Append("null");
        }
    }

    public override void Visit(Field field, UInt32OptionalType type, ref uint? value)
    {
        CheckFirst();
        if (value.HasValue)
        {
            sb.Append(value.Value);
        }
        else
        {
            sb.Append("null");
        }
    }

    public override void Visit(Field field, UInt64OptionalType type, ref ulong? value)
    {
        CheckFirst();
        if (value.HasValue)
        {
            sb.Append(value.Value);
        }
        else
        {
            sb.Append("null");
        }
    }

    public override void Visit(Field field, StringOptionalType type, ref string? value)
    {
        CheckFirst();
        if (value != null)
        {
            sb.Append('\'');
            sb.Append(value);
            sb.Append('\'');
        }
        else
        {
            sb.Append("null");
        }
    }

    public override void Visit(Field field, BoolOptionalType type, ref bool? value)
    {
        CheckFirst();
        if (value.HasValue)
        {
            sb.Append(value.Value);
        }
        else
        {
            sb.Append("null");
        }
    }

    public override void Visit(Field field, CharOptionalType type, ref char? value)
    {
        CheckFirst();
        if (value.HasValue)
        {
            sb.Append(value.Value);
        }
        else
        {
            sb.Append("null");
        }
    }

    public override void Visit(Field field, DateTimeType type, ref DateTime value)
    {
        CheckFirst();
        sb.Append('\'');
        sb.Append(value.ToString("o", System.Globalization.CultureInfo.InvariantCulture));
        sb.Append('\'');
    }

    public override void Visit(Field field, DateTimeOptionalType type, ref DateTime? value)
    {
        CheckFirst();
        if (value.HasValue)
        {
            sb.Append('\'');
            sb.Append(value.Value.ToString("o", System.Globalization.CultureInfo.InvariantCulture));
            sb.Append('\'');
        }
        else
        {
            sb.Append("null");
        }
    }

    public override void Visit(Field field, TimeSpanType type, ref TimeSpan value)
    {
        CheckFirst();
        sb.Append('\'');
        sb.Append(value.ToString("c", System.Globalization.CultureInfo.InvariantCulture));
        sb.Append('\'');
    }

    public override void Visit(Field field, TimeSpanOptionalType type, ref TimeSpan? value)
    {
        CheckFirst();
        if (value.HasValue)
        {
            sb.Append('\'');
            sb.Append(value.Value.ToString("c", System.Globalization.CultureInfo.InvariantCulture));
            sb.Append('\'');
        }
        else
        {
            sb.Append("null");
        }
    }

    public override void Visit(Field field, DateOnlyType type, ref DateOnly value)
    {
        CheckFirst();
        sb.Append('\'');
        sb.Append(value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        sb.Append('\'');
    }

    public override void Visit(Field field, DateOnlyOptionalType type, ref DateOnly? value)
    {
        CheckFirst();
        if (value.HasValue)
        {
            sb.Append('\'');
            sb.Append(
                value.Value.ToString(
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture
                )
            );
            sb.Append('\'');
        }
        else
        {
            sb.Append("null");
        }
    }

    public override void Visit(Field field, TimeOnlyType type, ref TimeOnly value)
    {
        CheckFirst();
        sb.Append('\'');
        sb.Append(value.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
        sb.Append('\'');
    }

    public override void Visit(Field field, TimeOnlyOptionalType type, ref TimeOnly? value)
    {
        CheckFirst();
        if (value.HasValue)
        {
            sb.Append('\'');
            sb.Append(
                value.Value.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
            );
            sb.Append('\'');
        }
        else
        {
            sb.Append("null");
        }
    }

    public override void VisitUnknown(Field field, IFieldType type)
    {
        // This method is not implemented in this visitor
    }

    public override void BeginArray(Field field, ArrayType fieldType)
    {
        CheckFirst();
        _first = true;
        sb.Append('[');
    }

    public override void EndArray()
    {
        _first = false;
        sb.Append(']');
    }

    public override void BeginStruct(Field field, StructType type)
    {
        CheckFirst();
        _first = true;
        sb.Append('{');
    }

    public override void EndStruct()
    {
        _first = false;
        sb.Append('}');
    }

    public override void BeginOptionalStruct(
        Field field,
        OptionalStructType type,
        bool isPresent,
        out bool createNew
    )
    {
        CheckFirst();
        _first = true;
        if (isPresent)
        {
            sb.Append('{');
        }
        else
        {
            sb.Append("null");
        }

        createNew = false;
    }

    public override void EndOptionalStruct(bool isPresent)
    {
        _first = false;
        if (isPresent)
        {
            sb.Append('}');
        }
    }

    public override void BeginList(Field field, ListType type, ref uint size)
    {
        CheckFirst();
        _first = true;
        sb.Append('<');
    }

    public override void EndList()
    {
        _first = false;
        sb.Append('>');
    }
}
