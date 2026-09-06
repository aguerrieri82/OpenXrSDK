using XrEngine;
using XrEngine.Audio.Midi;
using XrEngine.Devices;
using XrEngine.OpenXr;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Midi")]
        public static XrEngineAppBuilder CreateMidi(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var manager = Context.Require<IMidiManager>();

            var devices = manager.FindDevices();

            var usb = devices.FirstOrDefault(a => a.Name == "USB MIDI Interface" && a.Id!.StartsWith("in"));

            if (usb == null)
                usb = devices[0];

            var device = manager.GetDevice(usb.Id!);

            device!.OpenAsync().Wait();

            var inPort = device.OpenInput(0);
            inPort.DataReceived += (sender, e) =>
            {
                var span = new ReadOnlySpan<byte>(e.Data, e.Offset, e.Count);
                var msg = MidiMessageDecoder.Decode(span);
                if (msg is ActiveSensingMessage)
                    return;
                if (msg != null)
                    Log.Info(typeof(SampleScenes), $"MIDI Message: {msg}");
            };

            return builder
                .UseApp(app)
                //.UseEnvironmentDepth()
                //.UseDefaultHDR()
                .ConfigureSampleApp();
        }
    }
}
