using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Kaya.GrpcExplorer.Models;

/// <summary>
/// Dynamic message implementation for messages from gRPC reflection without compiled types
/// </summary>
internal class DynamicProtobufMessage(MessageDescriptor descriptor, byte[] data) : IMessage
{
    private readonly byte[] _data = data;

    public MessageDescriptor Descriptor { get; } = descriptor;

    public byte[] GetBytes() => _data;

    public void MergeFrom(CodedInputStream input)
    {
        // Not implemented for this dynamic use case
        throw new NotImplementedException();
    }

    public void WriteTo(CodedOutputStream output)
    {
        // Parse our bytes and rewrite each tag/value
        using var input = new CodedInputStream(_data);
        while (!input.IsAtEnd)
        {
            var tag = input.ReadTag();
            output.WriteTag(tag);
            
            var wireType = WireFormat.GetTagWireType(tag);
            switch (wireType)
            {
                case WireFormat.WireType.Varint:
                    output.WriteUInt64(input.ReadUInt64());
                    break;
                case WireFormat.WireType.Fixed64:
                    output.WriteFixed64(input.ReadFixed64());
                    break;
                case WireFormat.WireType.LengthDelimited:
                    var bytes = input.ReadBytes();
                    output.WriteBytes(bytes);
                    break;
                case WireFormat.WireType.Fixed32:
                    output.WriteFixed32(input.ReadFixed32());
                    break;
            }
        }
    }

    public int CalculateSize()
    {
        return _data.Length;
    }

    public bool Equals(IMessage? other)
    {
        return other is DynamicProtobufMessage msg && 
               msg.Descriptor == Descriptor && 
               msg._data.SequenceEqual(_data);
    }

    public IMessage Clone()
    {
        return new DynamicProtobufMessage(Descriptor, (byte[])_data.Clone());
    }
}
