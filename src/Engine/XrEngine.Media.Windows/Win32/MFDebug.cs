using System.Diagnostics;

namespace XrEngine.Media.Windows
{
    public static class MFDebug
    {
        public static void DumpMediaType(IMFMediaType mt)
        {
            mt.GetCount(out var count);
            for (uint i = 0; i < count; i++)
            {
                var value = new PropVariant();
                //mt.GetItemByIndex(i, out var key, ref value);
                //Debug.WriteLine(key);
            }

            if (mt == null)
            {

                Debug.WriteLine("MediaType = null");
                return;
            }

            // Fetch values safely (no PROPVARIANT needed)
            if (TryGetGUID(mt, MFAttributesGuid.MajorType, out var major))
                Debug.WriteLine($"MajorType: {MajorTypeToString(major)}");

            if (TryGetGUID(mt, MFAttributesGuid.Subtype, out var subtype))
                Debug.WriteLine($"Subtype: {SubtypeToString(subtype)}");

            if (TryGetUINT32(mt, MFAttributesGuid.AudioNumChannels, out var ch))
                Debug.WriteLine($"Channels: {ch}");

            if (TryGetUINT32(mt, MFAttributesGuid.AudioSamplesPerSecond, out var rate))
                Debug.WriteLine($"SampleRate: {rate}");

            if (TryGetUINT32(mt, MFAttributesGuid.AudioBitsPerSample, out var bps))
                Debug.WriteLine($"BitsPerSample: {bps}");

            if (TryGetUINT32(mt, MFAttributesGuid.AudioAvgBytesPerSecond, out var avg))
                Debug.WriteLine($"AvgBytesPerSec: {avg}");

            if (TryGetUINT32(mt, MFAttributesGuid.AudioBlockAlignment, out var align))
                Debug.WriteLine($"BlockAlign: {align}");
        }

        // ---------- Helpers ----------

        private static bool TryGetUINT32(IMFMediaType mt, Guid key, out uint value)
        {
            try
            {
                value = 0;
                return mt.GetUINT32(ref key, out value) == 0;

            }
            catch
            {
                value = 0;
                return false;
            }
        }

        private static bool TryGetGUID(IMFMediaType mt, Guid key, out Guid value)
        {
            try
            {
                value = default;
                return mt.GetGUID(ref key, out value) == 0;

            }
            catch
            {
                value = Guid.Empty;
                return false;
            }
        }

        private static string MajorTypeToString(Guid g)
        {
            // Standard MF types from mfmtypes.h
            if (g == MFMajorTypes.Audio) return "Audio";
            if (g == MFMajorTypes.Video) return "Video";
            return g.ToString();
        }

        private static string SubtypeToString(Guid g)
        {
            // Common audio subtypes
            if (g == MFSubtypes.PCM) return "PCM";
            if (g == MFSubtypes.Float) return "IEEE_FLOAT";

            return g.ToString();
        }
    }
}
