using System;
using System.Text;
using Godot;

namespace PCE.Util;

public enum WavAudioFormat {
    PCM = 1
}

public class WavLoader {
    private readonly FileAccess file;
    private readonly AudioStreamWav audioStream;
    public WavLoader(string path) {
        file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        audioStream = new();
        ParseHeader();
    }

    public AudioStreamWav GetStream() => audioStream;

    // RIFF  header
    // 0..4  bytes(big): ascii RIFF
    // 4..8  chunk_size(little): u32
    // 8..16 bytes(big): ascii WAVE
    private void ParseHeader() {
        ASCIIEncoding ascii = new();

        string riff = ascii.GetString(file.GetBuffer(4));
        if (riff != "RIFF")
            throw new FormatException($"invalid wav data (expected header to start with \"RIFF\", but it starts with \"{riff}\")");

        uint chunkSize = ReadLittleEndianUInt32();
        if (chunkSize != file.GetLength() - 8)
            throw new FormatException($"invalid wav data (chunksize is expected to be {file.GetLength() - 8} but it is {chunkSize})");

        string wave = ascii.GetString(file.GetBuffer(4));
        if (wave != "WAVE")
            throw new FormatException($"invalid wav data (expected next 4 bytes to be \"WAVE\", but it is \"{wave}\")");

        // the space at the end is important
        // because fmt is only 3 characters long
        string subChunk1ID = ascii.GetString(file.GetBuffer(4));
        if (subChunk1ID != "fmt ")
            throw new FormatException($"invalid wav data (expected subchunk1 ID to be \"fmt \", but it is \"{subChunk1ID}\")");

        uint subChunk1Size = ReadLittleEndianUInt32();

        WavAudioFormat audioFormat = ReadLittleEndianUInt16() switch {
            1 => WavAudioFormat.PCM,
            _ =>throw new FormatException("this wav loader does not support loading compressed wav files")
        };

        ushort numChannels = ReadLittleEndianUInt16();
        if (numChannels > 2)
            throw new FormatException($"invalid vaw data (only up to 2 channels are supported but this file has {numChannels})");

        // 44100: commonly used
        // 48000: professional digital audio standard
        uint sampleRate = ReadLittleEndianUInt32();

        uint byteRate = ReadLittleEndianUInt32();

        ushort blockAlign = ReadLittleEndianUInt16();

        ushort bitsPerSample = ReadLittleEndianUInt16();

        uint expectedByteRate = sampleRate * numChannels * bitsPerSample / 8;
        if (byteRate != expectedByteRate)
            throw new FormatException($"invalid wav data (expected byte rate to be {expectedByteRate}, but it is {byteRate})");

        ushort expectedBlockAlign = (ushort)(numChannels * bitsPerSample / 8);
        if (blockAlign != expectedBlockAlign)
            throw new FormatException($"invalid wav data (expected block align to be {expectedBlockAlign}, but it is {blockAlign})");

        for (string blockHeader = ascii.GetString(file.GetBuffer(4)); blockHeader != "data"; blockHeader = ascii.GetString(file.GetBuffer(4))) {
            GD.PushWarning($"Ignoring block {blockHeader}");
            uint blockSize = file.Get32();

            if (file.EofReached())
                throw new FormatException("invalid wav data (EOF reached)");

            file.Seek(file.GetPosition() + blockSize);
        }

        uint size = ReadLittleEndianUInt32();

        audioStream.Format = bitsPerSample switch {
            8 => AudioStreamWav.FormatEnum.Format8Bits,
            16 => AudioStreamWav.FormatEnum.Format16Bits,
            _ => throw new FormatException($"invalid wav data (unsupported bits/sample: {bitsPerSample})")
        };
        audioStream.MixRate = (int)sampleRate;
        audioStream.Stereo = numChannels == 2;
        audioStream.Data = file.GetBuffer((long)(file.GetLength() - (chunkSize - size) - 8));
    }

    private uint ReadLittleEndianUInt32() {
        byte[] bytes = file.GetBuffer(4);
        // reverse it
        // (though, c# already does that)
        if (!BitConverter.IsLittleEndian)
            bytes = [bytes[3], bytes[2], bytes[1], bytes[0]];

        return BitConverter.ToUInt32(bytes);
    }

    private ushort ReadLittleEndianUInt16() {
        byte[] bytes = file.GetBuffer(2);
        // reverse it
        // (though, c# already does that)
        if (!BitConverter.IsLittleEndian)
            bytes = [bytes[1], bytes[0]];

        return BitConverter.ToUInt16(bytes);
    }
}