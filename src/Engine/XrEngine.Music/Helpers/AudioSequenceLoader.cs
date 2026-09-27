using System.Security.Cryptography;
using System.Text;

namespace XrEngine.Music
{
    public class AudioSequenceLoader
    {
        public AudioSequencer Load(AudioSequenceInfo sequence, string? sourcePath = null)
        {
            ArgumentNullException.ThrowIfNull(sequence);

            var sequencer = new AudioSequencer();

            if (sequence.Tracks == null)
                return sequencer;

            foreach (var trackInfo in sequence.Tracks)
                sequencer.AddTrack(LoadTrack(trackInfo, sourcePath));

            sequencer.Id = sequence.Id;

            return sequencer;
        }

        protected virtual AudioTrack LoadTrack(AudioTrackInfo info, string? sourcePath)
        {
            var path = ResolveContent(info, sourcePath);

            AudioTrack result = info.Type switch
            {
                AudioTrackType.Wave => WaveAudioTrack.Load(path, CachePath),
                AudioTrackType.Drum => DrumAudioTrack.LoadSequence(path),
                AudioTrackType.Midi => throw new NotSupportedException("MIDI tracks are not supported."),
                _ => throw new NotSupportedException($"Unsupported audio track type '{info.Type}'.")
            };

            result.Id = info.Id;

            return result;
        }

        protected virtual string ResolveContent(AudioTrackInfo info, string? sourcePath)
        {
            if (info.Content != null)
                return MaterializeContent(info);

            if (string.IsNullOrWhiteSpace(info.ContentUri))
                throw new InvalidOperationException("Audio track has no content.");

            if (Path.IsPathRooted(info.ContentUri))
                return info.ContentUri;

            if (!string.IsNullOrWhiteSpace(sourcePath))
                return Path.GetFullPath(Path.Combine(sourcePath, info.ContentUri));

            return Context.Require<IAssetStore>().GetPath(info.ContentUri);
        }

        protected virtual string MaterializeContent(AudioTrackInfo info)
        {
            var data = GetContentData(info);
            var hash = Convert.ToHexString(SHA256.HashData(data));
            var extension = GetExtension(info);

            var path = Path.Combine(CachePath!, hash + extension);

            if (File.Exists(path))
                return path;

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, data);

            return path;
        }

        protected virtual byte[] GetContentData(AudioTrackInfo info)
        {
            if (info.Content is byte[] data)
                return data;

            if (info.Content is string text)
            {
                if (info.Type == AudioTrackType.Wave)
                    return Convert.FromBase64String(text);

                return Encoding.UTF8.GetBytes(text);
            }

            throw new NotSupportedException($"Unsupported embedded content type '{info.Content?.GetType().FullName}'.");
        }

        protected virtual string GetExtension(AudioTrackInfo info)
        {
            if (!string.IsNullOrWhiteSpace(info.ContentUri))
            {
                var extension = Path.GetExtension(info.ContentUri);

                if (!string.IsNullOrWhiteSpace(extension))
                    return extension;
            }

            return info.MimeType switch
            {
                "application/json" => ".json",
                "audio/mpeg" => ".mp3",
                "audio/wav" => ".wav",
                "audio/x-wav" => ".wav",
                "audio/ogg" => ".ogg",
                "audio/flac" => ".flac",
                _ => info.Type switch
                {
                    AudioTrackType.Drum => ".json",
                    AudioTrackType.Midi => ".mid",
                    _ => ".bin"
                }
            };
        }


        public string? CachePath { get; set; }
    }
}