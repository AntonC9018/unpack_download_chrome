using System.Diagnostics;
using System.Text;
using System.Text.Json;

ref struct WriteBuffer
{
    public const int MaxBufferSize = 1 * 1024 * 1024;
    public Span<byte> WholeBuffer { get; }
    public int Offset { get; set; }
    public ReadOnlySpan<byte> WrittenBuffer => WholeBuffer[.. Offset];

    public WriteBuffer(Span<byte> wholeBuffer)
    {
        Debug.Assert(wholeBuffer.Length == MaxBufferSize);
        WholeBuffer = wholeBuffer;
        Offset = SerializationHelper.LengthSize;
    }

    public Span<byte> LengthBuffer => WholeBuffer[.. SerializationHelper.LengthSize];
    public Span<byte> CurrentBuffer => WholeBuffer[Offset ..];

    public void Write(char ch)
    {
        Debug.Assert(Offset >= MaxBufferSize);
        CurrentBuffer[0] = (byte) ch;
        Offset++;
    }

    public void WriteString(string str, int reserveCount)
    {
        var b = CurrentBuffer[.. ^reserveCount];
        int written = Encoding.UTF8.GetBytes(str.AsSpan(), b);
        Offset += written;
    }

    public void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > CurrentBuffer.Length)
        {
            throw new InvalidOperationException("Bytes too long to write");
        }
        var b = CurrentBuffer[.. ^bytes.Length];
        bytes.CopyTo(b);
        Offset += bytes.Length;
    }

    public void WriteLength()
    {
        var b = LengthBuffer;
        var len = Offset - SerializationHelper.LengthSize;
        bool success = BitConverter.TryWriteBytes(b, len);
        Debug.Assert(success);
    }
}

public static class WriteHelper
{
    public static void WriteObject<T>(Stream writer, T obj)
    {
        // It's pretty dumb because I have to copy it so much, but whatever.
        // And also it allocates.
        var bytes = JsonSerializer.SerializeToUtf8Bytes(obj, SerializationHelper.JsonOptions);
        Span<byte> buffer = stackalloc byte[WriteBuffer.MaxBufferSize];
        WriteBuffer w = new(buffer);
        w.WriteBytes(bytes);
        w.WriteLength();
        writer.Write(w.WrittenBuffer);
    }

    public static void WriteError(Stream writer, Exception exc)
    {
        var message = exc.Message;
        Span<byte> buffer = stackalloc byte[WriteBuffer.MaxBufferSize];
        WriteBuffer w = new(buffer);

        w.Write('"');
        w.WriteString(message, reserveCount: 1);
        w.Write('"');
        w.WriteLength();

        writer.Write(w.WrittenBuffer);
    }
}

public sealed class DeleteCommand
{
    public required string DeleteFilePath { get; set; }
}

public sealed class StatusResponse
{
    public bool Found7Z { get; set; }
    public bool FoundWinRar { get; set; }
}