using System.Buffers.Binary;
using System.Text;
using CalisApi.Common;

namespace CalisApi.Services.Storage;

/// <summary>Comprueba firmas reales y estructura MP4 (duración y codecs), sin confiar en metadatos del cliente.</summary>
public static class MediaFileValidator
{
    public static void Validate(byte[] bytes, string contentType, int maxSeconds = 120)
    {
        var data = bytes.AsSpan();
        var valid = contentType switch
        {
            "image/jpeg" => data.Length >= 4 && data[0] == 0xff && data[1] == 0xd8 && data[2] == 0xff
                && data[^2] == 0xff && data[^1] == 0xd9,
            "image/png" => data.Length >= 33 && data[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
                && data.Slice(12, 4).SequenceEqual("IHDR"u8) && BinaryPrimitives.ReadUInt32BigEndian(data[16..]) > 0
                && BinaryPrimitives.ReadUInt32BigEndian(data[20..]) > 0,
            "image/webp" => data.Length >= 20 && data[..4].SequenceEqual("RIFF"u8) && data.Slice(8, 4).SequenceEqual("WEBP"u8)
                && BinaryPrimitives.ReadUInt32LittleEndian(data[4..]) == data.Length - 8
                && (data.Slice(12, 4).SequenceEqual("VP8 "u8) || data.Slice(12, 4).SequenceEqual("VP8L"u8) || data.Slice(12, 4).SequenceEqual("VP8X"u8)),
            "video/mp4" => ValidateMp4(bytes, maxSeconds),
            _ => false,
        };
        if (!valid) throw new AppException($"El archivo no corresponde a su formato. Usa JPG, PNG, WebP o MP4 H.264/AAC de hasta {maxSeconds} segundos.");
    }

    private static bool ValidateMp4(byte[] data, int maxSeconds)
    {
        try
        {
            var roots = Boxes(data, 0, data.Length);
            if (!roots.Any(b => b.Type == "ftyp" && b.Length >= 8) || !roots.Any(b => b.Type == "mdat" && b.Length > 0)) return false;
            var movie = roots.Single(b => b.Type == "moov");
            var children = Boxes(data, movie.Offset, movie.Length);
            if (!ValidDuration(data, children.Single(b => b.Type == "mvhd"), maxSeconds)) return false;
            var hasVideo = false;
            foreach (var track in children.Where(b => b.Type == "trak"))
            {
                var media = Child(data, track, "mdia");
                if (!ValidDuration(data, Child(data, media, "mdhd"), maxSeconds)) return false;
                var handler = Child(data, media, "hdlr");
                if (handler.Length < 12) return false;
                var type = Encoding.ASCII.GetString(data, handler.Offset + 8, 4);
                var descriptions = Child(data, Child(data, Child(data, media, "minf"), "stbl"), "stsd");
                if (descriptions.Length < 8) return false;
                var samples = Boxes(data, descriptions.Offset + 8, descriptions.Length - 8);
                if (samples.Count == 0 || samples.Count != BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(descriptions.Offset + 4))) return false;
                if (type == "vide")
                {
                    if (samples.Any(s => s.Type is not ("avc1" or "avc3") || s.Length < 78)) return false;
                    hasVideo = true;
                }
                else if (type != "soun" || samples.Any(s => s.Type != "mp4a" || s.Length < 28)) return false;
            }
            return hasVideo;
        }
        catch (Exception ex) when (ex is InvalidDataException or InvalidOperationException or ArgumentOutOfRangeException or OverflowException)
        { return false; }
    }

    private static bool ValidDuration(byte[] bytes, Box box, int maxSeconds)
    {
        var data = bytes.AsSpan(box.Offset, box.Length);
        if (data.Length < 20) return false;
        double duration;
        uint scale;
        if (data[0] == 0)
        {
            scale = BinaryPrimitives.ReadUInt32BigEndian(data[12..]);
            duration = BinaryPrimitives.ReadUInt32BigEndian(data[16..]);
        }
        else if (data[0] == 1 && data.Length >= 32)
        {
            scale = BinaryPrimitives.ReadUInt32BigEndian(data[20..]);
            duration = BinaryPrimitives.ReadUInt64BigEndian(data[24..]);
        }
        else return false;
        return scale > 0 && duration > 0 && duration / scale <= maxSeconds;
    }

    private static Box Child(byte[] bytes, Box parent, string type) =>
        Boxes(bytes, parent.Offset, parent.Length).Single(b => b.Type == type);

    private static List<Box> Boxes(byte[] data, int offset, int length)
    {
        var result = new List<Box>();
        var end = checked(offset + length);
        while (offset < end)
        {
            if (end - offset < 8 || result.Count >= 10000) throw new InvalidDataException();
            long size = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset));
            var type = Encoding.ASCII.GetString(data, offset + 4, 4);
            var header = 8;
            if (size == 1)
            {
                if (end - offset < 16) throw new InvalidDataException();
                size = checked((long)BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(offset + 8)));
                header = 16;
            }
            if (size == 0) size = end - offset;
            if (size < header || size > end - offset) throw new InvalidDataException();
            result.Add(new(type, offset + header, (int)size - header));
            offset += (int)size;
        }
        return result;
    }

    private readonly record struct Box(string Type, int Offset, int Length);
}
