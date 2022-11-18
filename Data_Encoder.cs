using System.Net;
using System.Text;

namespace LAN_Game
{
    public static class Data_Encoder
    {
        public struct DecodeResult<T>
        {
            public readonly T Result;
            public readonly int EndIndex;

            public DecodeResult(T result, int end)
            {
                Result = result;
                EndIndex = end;
            }

            public static implicit operator T(DecodeResult<T> result)
            {
                return result.Result;
            }

            public override string ToString()
            {
                return $"{Result}; End={EndIndex}";
            }
        }

        #region Encoders

        public static byte[] Encode(this object value)
        {
            if (value is string @string)
                return @string.Encode();
            if (value is int @int)
                return @int.Encode();
            if (value is ushort @ushort)
                return @ushort.Encode();
            if (value is double @double)
                return @double.Encode();

            if (value is IEnumerable<byte> bytes)
                return bytes.Encode();
            if (value is LAN_Message message)
                return message.Encode();
            if (value is LAN_Member member)
                return member.Encode();
            if (value is LAN_Member[] members)
                return members.Encode();

            throw new NotSupportedException(nameof(value));
        }

        public static byte[] Encode(this byte value)
            => new byte[] { value };

        public static byte[] Encode(this ushort value)
            => BitConverter.GetBytes(value);

        public static byte[] Encode(this int value)
            => BitConverter.GetBytes(value);

        public static byte[] Encode(this double value)
            => BitConverter.GetBytes(value);

        public static byte[] Encode(this string value)
        {
            if (value.Length > 255)
                throw new ArgumentException("String is too long!", nameof(value));

            byte[] data = Encoding.UTF8.GetBytes(value);
            byte len = (byte)value.Length;
            return len.Encode().Concat(data).ToArray();
        }


        public static byte[] Encode(this IEnumerable<byte> value)
        {
            int len = value.Count();
            if (len > ushort.MaxValue)
                throw new ArgumentException("Data is too long!", nameof(value));

            ushort length = (ushort)len;
            return length.Encode().Concat(value).ToArray();
        }

        public static byte[] Encode(this LAN_Message value)
        {
            byte[] type_bytes = value.MessageType.Encode();
            byte[] host_bytes = value.Hostname.Encode();
            byte[] port_bytes = value.Port.Encode();
            byte[] data_bytes = value.Data.Encode();

            return type_bytes.Concat(host_bytes).Concat(port_bytes).Concat(data_bytes).ToArray();
        }

        public static byte[] Encode(this LAN_Member value)
        {
            byte[] name_bytes = value.Name.Encode();
            byte[] address_bytes = value.Address.GetAddressBytes();
            byte[] port_bytes = value.Port.Encode();

            return name_bytes.Concat(address_bytes).Concat(port_bytes).ToArray();
        }

        public static byte[] Encode(this LAN_Member[] value)
        {
            byte[] result_bytes = Encode((ushort)value.Length);
            foreach (var member in value)
                result_bytes = result_bytes.Concat(member.Encode()).ToArray();

            return result_bytes;
        }


        #endregion
        #region Decoders

        public static DecodeResult<byte> Decode_Byte(this byte[] value, int start = 0)
            => new(value[start], start + 1);

        public static DecodeResult<ushort> Decode_UShort(this byte[] value, int start = 0)
            => new(BitConverter.ToUInt16(value, start), start + sizeof(ushort));

        public static DecodeResult<int> Decode_Int(this byte[] value, int start = 0)
            => new(BitConverter.ToInt32(value, start), start + sizeof(int));

        public static DecodeResult<double> Decode_Double(this byte[] value, int start = 0)
            => new(BitConverter.ToDouble(value, start), start + sizeof(double));

        public static DecodeResult<string> Decode_String(this byte[] value, int start = 0)
        {
            var len = value.Decode_Byte(start);
            return new(Encoding.UTF8.GetString(value, len.EndIndex, len.Result), len.EndIndex + len.Result);
        }

        public static DecodeResult<byte[]> Decode_Bytes(this byte[] value, int start = 0)
        {
            var len = value.Decode_UShort(start);
            byte[] data = value.Skip(len.EndIndex).Take(len).ToArray();
            return new DecodeResult<byte[]>(data, len.EndIndex + len.Result);
        }

        public static DecodeResult<LAN_Message> Decode_LAN_Message(this byte[] value, int start = 0)
        {
            var type = value.Decode_Byte(start);
            var host_name = value.Decode_String(type.EndIndex);
            var port = value.Decode_UShort(host_name.EndIndex);
            var data = value.Decode_Bytes(port.EndIndex);

            return new(new LAN_Message(type, host_name, port, data.Result), port.EndIndex);
        }

        public static DecodeResult<LAN_Member> Decode_LAN_Member(this byte[] value, int start = 0)
        {
            var name = value.Decode_String(start);
            var address_bytes = value.Skip(name.EndIndex).Take(4).ToArray();
            var address = new IPAddress(address_bytes);
            var port = value.Decode_UShort(name.EndIndex + 4);

            return new(new(name, address, port), port.EndIndex);
        }

        public static DecodeResult<LAN_Member[]> Decode_LAN_Members(this byte[] value, int start = 0)
        {
            var count = value.Decode_UShort(start);
            int end_index = count.EndIndex;
            LAN_Member[] result = new LAN_Member[count];

            for (int i = 0; i < count; i++)
                result = result.Append(value.Decode_LAN_Member(end_index)).ToArray();

            return new(result, end_index);
        }

        #endregion
    }
}
