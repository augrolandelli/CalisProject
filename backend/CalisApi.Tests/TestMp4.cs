using System.Buffers.Binary;
using System.Text;

namespace CalisApi.Tests;

/// <summary>Fixture de metadatos ISO-BMFF para pruebas: no decodificamos video; verificamos las reglas del contenedor.</summary>
public static class TestMp4
{
    public static byte[] Movie(uint seconds, string codec = "avc1")
    {
        var duration = new byte[24];
        BinaryPrimitives.WriteUInt32BigEndian(duration.AsSpan(12), 1000);
        BinaryPrimitives.WriteUInt32BigEndian(duration.AsSpan(16), seconds * 1000);
        var handler = new byte[12];
        Encoding.ASCII.GetBytes("vide").CopyTo(handler, 8);
        var descriptionHeader = new byte[8];
        BinaryPrimitives.WriteUInt32BigEndian(descriptionHeader.AsSpan(4), 1);
        return Box("ftyp", "isom0000"u8.ToArray())
            .Concat(Box("moov", Box("mvhd", duration), Box("trak", Box("mdia",
                Box("mdhd", duration), Box("hdlr", handler), Box("minf", Box("stbl", Box("stsd", descriptionHeader, Box(codec, new byte[78]))))))))
            .Concat(Box("mdat", new byte[16])).ToArray();
    }

    private static byte[] Box(string type, params byte[][] contents)
    {
        var payload = contents.SelectMany(c => c).ToArray();
        var data = new byte[8 + payload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(data, (uint)data.Length);
        Encoding.ASCII.GetBytes(type).CopyTo(data, 4);
        payload.CopyTo(data, 8);
        return data;
    }
}
