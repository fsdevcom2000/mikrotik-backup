using System.Buffers;
using System.Text;

namespace MikroTikBackup.RouterOS.Protocol;

public static class RouterOsProtocol
{
    public static byte[] EncodeWord(string word)
    {
        var data = Encoding.UTF8.GetBytes(word);

        using var stream = new MemoryStream();

        WriteLength(stream, data.Length);
        stream.Write(data);

        return stream.ToArray();
    }

    public static byte[] EncodeSentence(
        IEnumerable<string> words)
    {
        using var stream = new MemoryStream();

        foreach (var word in words)
        {
            var data = Encoding.UTF8.GetBytes(word);

            WriteLength(stream, data.Length);
            stream.Write(data);
        }

        // Zero-length word terminates the sentence.
        stream.WriteByte(0);

        return stream.ToArray();
    }

    public static async Task<string?> ReadWordAsync(
        Func<Memory<byte>, CancellationToken, Task<int>> reader,
        CancellationToken cancellationToken)
    {
        var length = await ReadLengthAsync(
            reader,
            cancellationToken);

        if (length == 0)
            return null;

        var buffer = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            await ReadExactlyAsync(
                reader,
                buffer.AsMemory(0, length),
                cancellationToken);

            return Encoding.UTF8.GetString(
                buffer,
                0,
                length);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async Task<int> ReadLengthAsync(
        Func<Memory<byte>, CancellationToken, Task<int>> reader,
        CancellationToken cancellationToken)
    {
        var firstBuffer = new byte[1];

        await ReadExactlyAsync(
            reader,
            firstBuffer,
            cancellationToken);

        var first = firstBuffer[0];

        if ((first & 0x80) == 0)
            return first;

        if ((first & 0xC0) == 0x80)
        {
            var buffer = new byte[1];

            await ReadExactlyAsync(
                reader,
                buffer,
                cancellationToken);

            return ((first & 0x3F) << 8) | buffer[0];
        }

        if ((first & 0xE0) == 0xC0)
        {
            var buffer = new byte[2];

            await ReadExactlyAsync(
                reader,
                buffer,
                cancellationToken);

            return
                ((first & 0x1F) << 16) |
                (buffer[0] << 8) |
                buffer[1];
        }

        if ((first & 0xF0) == 0xE0)
        {
            var buffer = new byte[3];

            await ReadExactlyAsync(
                reader,
                buffer,
                cancellationToken);

            return
                ((first & 0x0F) << 24) |
                (buffer[0] << 16) |
                (buffer[1] << 8) |
                buffer[2];
        }

        if (first == 0xF0)
        {
            var buffer = new byte[4];

            await ReadExactlyAsync(
                reader,
                buffer,
                cancellationToken);

            return
                (buffer[0] << 24) |
                (buffer[1] << 16) |
                (buffer[2] << 8) |
                buffer[3];
        }

        throw new InvalidDataException(
            $"Invalid RouterOS API word length prefix: 0x{first:X2}");
    }

    private static async Task ReadExactlyAsync(
        Func<Memory<byte>, CancellationToken, Task<int>> reader,
        Memory<byte> buffer,
        CancellationToken cancellationToken)
    {
        var totalRead = 0;

        while (totalRead < buffer.Length)
        {
            var read = await reader(
                buffer[totalRead..],
                cancellationToken);

            if (read == 0)
            {
                throw new EndOfStreamException(
                    "RouterOS closed the connection.");
            }

            totalRead += read;
        }
    }

    private static void WriteLength(
        Stream stream,
        int length)
    {
        if (length < 0x80)
        {
            stream.WriteByte((byte)length);
            return;
        }

        if (length < 0x4000)
        {
            stream.WriteByte(
                (byte)((length >> 8) | 0x80));

            stream.WriteByte(
                (byte)(length & 0xff));

            return;
        }

        if (length < 0x200000)
        {
            stream.WriteByte(
                (byte)((length >> 16) | 0xC0));

            stream.WriteByte(
                (byte)((length >> 8) & 0xff));

            stream.WriteByte(
                (byte)(length & 0xff));

            return;
        }

        if (length < 0x10000000)
        {
            stream.WriteByte(
                (byte)((length >> 24) | 0xE0));

            stream.WriteByte(
                (byte)((length >> 16) & 0xff));

            stream.WriteByte(
                (byte)((length >> 8) & 0xff));

            stream.WriteByte(
                (byte)(length & 0xff));

            return;
        }

        stream.WriteByte(0xF0);

        stream.WriteByte(
            (byte)((length >> 24) & 0xff));

        stream.WriteByte(
            (byte)((length >> 16) & 0xff));

        stream.WriteByte(
            (byte)((length >> 8) & 0xff));

        stream.WriteByte(
            (byte)(length & 0xff));
    }
}