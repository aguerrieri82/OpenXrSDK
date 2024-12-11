namespace Sfizz
{
    public static class Test
    {
        public static void Execute()
        {
            var cfg = new SfizzLib.Config
            {
                ChannelSizeSamples = 1024,
                NumVoices = 64,
                ProcessMode = SfizzLib.ProcessMode.ProcessLive,
                SampleQuality = 2,
                OscillatorQuality = 2,
                SampleRate = 48000
            };

            var synth = SfizzLib.createSynth();
            synth.configure(ref cfg);
            var buf = SfizzLib.createBuffer(cfg.ChannelSizeSamples, 2);
            synth.loadSfzFile("D:\\SoundFont\\Sm\\Programs\\SM_Drums_kit.sfz");

            synth.setVolume(2);
            /*
            var wOut = new WasapiOut();
            wOut.Init(new SfizzSampleProvider(synth, buf, cfg.BlockSize * 2, cfg.SampleRate, 2));
            wOut.Play();

            while (true)
            {
                synth.noteOn(0, 54, 127);
                Thread.Sleep(400);
            }
            */
        }
    }
}
