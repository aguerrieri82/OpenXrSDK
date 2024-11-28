using System.Runtime.InteropServices;

namespace Sfizz
{
    public static class SfizzLib
    {
        public enum ProcessMode
        {
            ProcessLive,
            ProcessFreewheeling,
        };

        public struct Config
        {
            public int BlockSize;
            public int SampleRate; 
            public ProcessMode ProcessMode;
            public int SampleQuality;
            public int NumVoices;
            public int OscillatorQuality;
        };

        public struct Synth
        {
            public nint Handle;
        }

        public struct Buffer
        {
            public nint Handle;
        }


        [DllImport("sfizz_api")]
        public static extern Synth createSynth();

        [DllImport("sfizz_api")]
        public static extern void deleteSynth(this Synth synth);

        [DllImport("sfizz_api")]
        public static extern void noteOn(this Synth synth, int delay, int noteNumber, int velocity);

        [DllImport("sfizz_api")]
        public static extern void noteOff(this Synth synth, int delay, int noteNumber, int velocity);


        [DllImport("sfizz_api")]
        public static extern void loadSfzFile(this Synth synth, string fileName);

        [DllImport("sfizz_api")]
        public static extern void setVolume(this Synth synth, float value);


        [DllImport("sfizz_api")]
        public static extern float getVolume(this Synth synth);


        [DllImport("sfizz_api")]
        public static extern void allSoundOff(this Synth synth);

        [DllImport("sfizz_api")]
        public static extern void controlCode(this Synth synth, int delay, int ccNumber, int ccValue);

        [DllImport("sfizz_api")]
        public static extern void configure(this Synth synth, ref Config config);


        [DllImport("sfizz_api")]
        public static extern Buffer createBuffer(long blockSize, int channels = 2);


        [DllImport("sfizz_api")]
        public static extern unsafe short* getPcmPointer(this Buffer buffer);

        [DllImport("sfizz_api")]
        public static extern unsafe float* getFloatPointer(this Buffer buffer);

        [DllImport("sfizz_api")]
        public static extern void deleteBuffer(this Buffer buffer);


        [DllImport("sfizz_api")]
        public static extern void render(this Synth synth, Buffer buffer, bool convertPcm);

    }
}
