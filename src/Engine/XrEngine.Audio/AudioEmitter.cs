using OpenAl.Framework;
using Silk.NET.Core.Native;
using Silk.NET.OpenAL;
using System;
using System.Numerics;

namespace XrEngine.Audio
{
    public class AudioEmitter : Behavior<Object3D>
    {
        static AlSourcePool? _pool;
        protected HashSet<IAudioStream> _activeStreams = [];

        public AlSource Play(AlBuffer buffer, Vector3 direction)
        {
            if (_pool == null)
            {
                var system = _host!.Scene!.Component<AudioSystem>();
                _pool = new AlSourcePool(system.Device.Al);
            }

            var source = _pool.Get(buffer);
            source.Position = _host!.WorldPosition;
            source.Direction = direction;

            source.Play();

            return source;
        }

        public async Task PlayAsync(IAudioStream stream, Func<Vector3> getDirection)
        {
            var al = AlDevice.Current!.Al;

            double bufferTime = 0.05f;
            var bufferCount = Math.Max(2, stream.PrefBufferCount);

            var bufferSizeBytes = (uint)(bufferTime * stream.Format.SampleRate * (stream.Format.BitsPerSample / 2));

            if (stream.PrefBufferSize > 0)
                bufferSizeBytes = stream.PrefBufferSize;

            var buffers = new AlBuffer[bufferCount];

            var bufferData = new byte[bufferSizeBytes];

            long curSamples = 0;

            void FillBuffer(AlBuffer toFill)
            {
                stream.Fill(bufferData, curSamples / (float)stream.Format.SampleRate);
                toFill.SetData(bufferData, stream.Format);
                curSamples += bufferSizeBytes / (stream.Format.BitsPerSample / 2);
            }

            for (var i = 0; i < bufferCount; i++)
            {
                buffers[i] = new AlBuffer(al);
                FillBuffer(buffers[i]);
            }

            using var source = new AlSource(al);
            source.QueueBuffer(buffers);

            _activeStreams.Add(stream);

            stream.Start();

            source.Play();

            while (stream.IsStreaming)
            {
                while (source.BuffersProcessed > 0)
                {
                    var buffer = source.DequeueBuffers(1).First();
                    FillBuffer(buffer);
                    source.Direction = getDirection();
                    source.QueueBuffer(buffer);

                    if (source.State == SourceState.Stopped)
                        source.Play();

                    source.Position = _host!.WorldPosition;
                }

                if (source.State == SourceState.Stopped)
                    source.Play();

                await Task.Delay(10);
            }

            _activeStreams.Remove(stream);

            source.Stop();

            foreach (var buffer in buffers)
                buffer.Dispose();   
        }

        public void Stop()
        {
            if (_activeStreams.Count == 0)
                return;

            foreach (var stream in _activeStreams.ToArray())
                stream.Stop();
        }
    }
}
