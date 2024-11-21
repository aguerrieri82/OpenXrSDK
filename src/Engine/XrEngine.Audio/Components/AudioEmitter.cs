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
        protected AlSource? _curSource;

        public AlSource Play(AlBuffer buffer, Vector3 direction)
        {
            if (_pool == null)
            {
                var system = _host!.Scene!.Component<AudioSystem>();
                _pool = new AlSourcePool(system.Device.Al);
            }

            _curSource = _pool.Get(buffer);
            _curSource.Position = _host!.WorldPosition;
            _curSource.Direction = direction;

            _curSource.Play();

            return _curSource;
        }

        public async Task PlayAsync(IAudioStream stream, Func<Vector3> getDirection)
        {
            var al = AlDevice.Current!.Al;

            double bufferTime = 0.05f;
            var bufferCount = Math.Max(2, stream.PrefBufferCount);

            var bufferSizeBytes = (uint)(bufferTime * stream.Format.SampleRate * (stream.Format.BitsPerSample / 8));

            if (stream.PrefBufferSize > 0)
                bufferSizeBytes = stream.PrefBufferSize;

            var buffers = new AlBuffer[bufferCount];

            var bufferData = new byte[bufferSizeBytes];

            long curSamples = 0;

            void FillBuffer(AlBuffer toFill)
            {
                stream.Fill(bufferData, curSamples / (float)stream.Format.SampleRate);
                toFill.SetData(bufferData, stream.Format);
                curSamples += bufferSizeBytes / (stream.Format.BitsPerSample / 8);
            }

            for (var i = 0; i < bufferCount; i++)
            {
                buffers[i] = new AlBuffer(al);
                FillBuffer(buffers[i]);
            }

            _curSource = new AlSource(al);
            _curSource.QueueBuffer(buffers);

            _activeStreams.Add(stream);

            stream.Start();

            _curSource.Play();

            while (stream.IsStreaming)
            {
                while (_curSource.BuffersProcessed > 0)
                {
                    var buffer = _curSource.DequeueBuffers(1).First();
                    FillBuffer(buffer);
                    _curSource.Direction = getDirection();
                    _curSource.QueueBuffer(buffer);

                    if (_curSource.State == SourceState.Stopped)
                        _curSource.Play();

                    _curSource.Position = _host!.WorldPosition;
                }

                if (_curSource.State == SourceState.Stopped)
                    _curSource.Play();

                await Task.Delay(10);
            }

            _activeStreams.Remove(stream);

            _curSource.Stop();
            _curSource.Dispose();
            _curSource = null;

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
