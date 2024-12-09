using OpenAl.Framework;
using Silk.NET.OpenAL;
using System.IO;
using System.Numerics;

namespace XrEngine.Audio
{
    public class AudioEmitter : Behavior<Object3D>
    {
        static AlSourcePool? _pool;
        protected HashSet<IAudioStream> _activeStreams = [];
        protected AlSource? _curSource;

        #region  ThreadAudioControl

        protected class ThreadAudioControl : IAudioControl
        {
            private readonly Thread _thread;

            public ThreadAudioControl(Thread thread)
            {
                _thread = thread;
            }

            public void Stop()
            {
                if (IsStopped)
                    return;
                IsStopped = true;
                _thread.Join();
            }

            public bool IsStopped { get; private set; }
        }


        #endregion

        public AudioEmitter()
        {
            PoolSleepMs = 0.1f;    
        }

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

        public IAudioControl Play(IAudioStream stream, Func<Vector3> getDirection)
        {
            var thread = new Thread(p => PlayWork(stream, getDirection, (ThreadAudioControl)p!));
            thread.Name = "Audio Stream Player";
            var control = new ThreadAudioControl(thread);
            thread.Start(control);
            return control;
        }

        protected void PlayWork(IAudioStream stream, Func<Vector3> getDirection, ThreadAudioControl control)
        {
            var al = AlDevice.Current!.Al;

            double bufferTime = 0.05f;
            var bufferCount = Math.Max(1, stream.PrefBufferCount);
            var bufferSizeBytes = (int)(bufferTime * stream.Format.SampleRate * (stream.Format.BitsPerSample / 8));

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

            while (stream.IsStreaming && !control.IsStopped)
            {
                while (_curSource.BuffersProcessed > 0)
                {
                    var buffer = _curSource.DequeueBuffers(1).First();

                    FillBuffer(buffer);

                    _curSource.Direction = getDirection();
                    _curSource.Position = Position ?? _host!.WorldPosition;
                    _curSource.QueueBuffer(buffer);

                    if (_curSource.State == SourceState.Stopped)
                        _curSource.Play();
                }

                if (_curSource.State == SourceState.Stopped)
                    _curSource.Play();

                EngineNativeLib.SleepFor((ulong)(PoolSleepMs * 1000000));
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

        public float PoolSleepMs { get; set; }

        public Vector3? Position { get; set; }

        public AlSource? ActiveSource => _curSource;
    }
}
