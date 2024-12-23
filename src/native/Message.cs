using System.Text;
using System.Text.Json;

public sealed class Message
{
    public string? FilePath { get; set; }
    public bool RefreshTools { get; set; }
}

public static class SerializationHelper
{
    public const int LengthSize = 4;
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}

public static class ReadHelper
{
    public static Message Read(Stream input)
    {
        Span<byte> lenBytes = stackalloc byte[SerializationHelper.LengthSize];
        input.ReadExactly(lenBytes);
        var len = BitConverter.ToInt32(lenBytes);

        using var reader = new StreamReader(
            encoding: Encoding.UTF8,
            leaveOpen: true,
            detectEncodingFromByteOrderMarks: false,
            stream: input);
        var bufferArr = new char[len];
        var bufferSpan = bufferArr.AsSpan();
        bool success = ReadChars(reader, bufferSpan);
        if (!success)
        {
            throw new InvalidOperationException("Invalid input stream.");
        }

        var message = JsonSerializer.Deserialize<Message>(bufferSpan, SerializationHelper.JsonOptions);
        if (message is null)
        {
            throw new InvalidOperationException("Huh? Passing null?");
        }
        return message;
    }

    public static bool ReadChars(StreamReader input, Span<char> bufferSpan)
    {
        int pos = 0;

        while (true)
        {
            var bufferSegment = bufferSpan[pos ..];
            if (bufferSegment.Length == 0)
            {
                return true;
            }
            int count = input.Read(bufferSegment);
            if (count == 0)
            {
                return false;
            }
            if (count == bufferSegment.Length)
            {
                return true;
            }
        }
    }
}