using System.Collections.Concurrent;
using System.IO;
using System.Media;
using TomsToolbox.Essentials;
using static RegionToShare.NativeMethods;

namespace RegionToShare;

/// <summary>
/// Plays click sounds: built in sounds are synthesized, any other value is treated as the path of a .wav file.
/// </summary>
internal static class ClickSounds
{
    public const string None = "";
    public const string High = "High";
    public const string Low = "Low";
    public const string Pop = "Pop";

    private const int SampleRate = 44100;

    private static SoundPlayer? _current;

    private static readonly ConcurrentDictionary<string, SoundPlayer?> Players = new(StringComparer.OrdinalIgnoreCase);

    public static bool IsBuiltIn(string? sound) => sound is High or Low or Pop;

    public static void Play(string? sound, int volumePercent)
    {
        if (sound.IsNullOrEmpty())
            return;

        _ = Task.Run(() => PlayInternal(sound, volumePercent));
    }

    private static void PlayInternal(string sound, int volumePercent)
    {
        var player = GetPlayer(sound);
        if (player == null)
            return;

        // Sets the volume of this application's audio session.
        var volume = (uint)(Math.Max(0, Math.Min(100, volumePercent)) * 0xFFFF / 100);
        waveOutSetVolume(IntPtr.Zero, volume | (volume << 16));

        try
        {
            // Stop a still playing sound, different players would overlap otherwise.
            Interlocked.Exchange(ref _current, player)?.Stop();
            player.Play();
        }
        catch
        {
            // Invalid or unsupported file, stay silent.
        }
    }

    private static SoundPlayer? GetPlayer(string sound)
    {
        try
        {
            return Players.GetOrAdd(sound, CreatePlayer);
        }
        catch
        {
            return null;
        }
    }

    private static SoundPlayer? CreatePlayer(string sound)
    {
        try
        {
            var player = sound switch
            {
                High => new SoundPlayer(Synthesize(2400, 2400, 0.025, 0.005)),
                Low => new SoundPlayer(Synthesize(900, 900, 0.040, 0.010)),
                Pop => new SoundPlayer(Synthesize(300, 1000, 0.045, 0.012)),
                _ => File.Exists(sound) ? new SoundPlayer(sound) : null
            };

            player?.Load();

            return player;
        }
        catch
        {
            // Do not cache missing files, they might show up later.
            if (!IsBuiltIn(sound))
                throw;

            return null;
        }
    }

    /// <summary>
    /// Creates a short, decaying tone as 16 bit mono PCM wave file, sweeping from <paramref name="startFrequency"/> to <paramref name="endFrequency"/>.
    /// </summary>
    private static Stream Synthesize(double startFrequency, double endFrequency, double duration, double decay)
    {
        var sampleCount = (int)(SampleRate * duration);
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);

        writer.Write("RIFF".ToCharArray());
        writer.Write(36 + sampleCount * 2);
        writer.Write("WAVE".ToCharArray());
        writer.Write("fmt ".ToCharArray());
        writer.Write(16);
        writer.Write((short)1); // PCM
        writer.Write((short)1); // mono
        writer.Write(SampleRate);
        writer.Write(SampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data".ToCharArray());
        writer.Write(sampleCount * 2);

        var phase = 0.0;

        for (var i = 0; i < sampleCount; i++)
        {
            var t = (double)i / SampleRate;
            var frequency = startFrequency + (endFrequency - startFrequency) * i / sampleCount;
            phase += 2 * Math.PI * frequency / SampleRate;

            var attack = Math.Min(1.0, t / 0.001);
            var envelope = attack * Math.Exp(-t / decay);

            writer.Write((short)(Math.Sin(phase) * envelope * 0.8 * short.MaxValue));
        }

        writer.Flush();
        stream.Position = 0;
        return stream;
    }
}
