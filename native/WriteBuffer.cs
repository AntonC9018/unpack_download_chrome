using System.Diagnostics;
using System.Text;

ref struct WriteBuffer
{
    public const int MaxBufferSize = 1 * 1024 * 1024;
    public const int LengthSize = 4;
    public Span<byte> WholeBuffer { get; }
    public int Offset { get; set; }

    public WriteBuffer(Span<byte> wholeBuffer)
    {
        Debug.Assert(wholeBuffer.Length == MaxBufferSize);
        WholeBuffer = wholeBuffer;
        Offset = LengthSize;
    }

    public Span<byte> LengthBuffer => WholeBuffer[.. LengthSize];
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

    public void WriteLength()
    {
        var b = LengthBuffer;
        var len = Offset - LengthSize;
        bool success = BitConverter.TryWriteBytes(b, len);
        Debug.Assert(success);
    }
}

public static class WriteHelper
{
    public static void WriteError(Stream writer, Exception exc)
    {
        var message = exc.Message;
        Span<byte> buffer = stackalloc byte[WriteBuffer.MaxBufferSize];
        WriteBuffer w = new(buffer);

        w.Write('"');
        w.WriteString(message, reserveCount: 1);
        w.Write('"');
        w.WriteLength();

        writer.Write(w.WholeBuffer);
    }
}