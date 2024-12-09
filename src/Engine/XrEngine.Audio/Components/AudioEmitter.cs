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

        #region  StreamControl

        protected class StreamControl : IAudioControl
        {
            private readonly Thread? _thread;

            public StreamControl(Thread? thread = null)
            {
                _thread = thread;
            }

            public void Stop()
            {
                if (IsStopped)
                    return;
                IsStopped = true;
                _thread?.Join();
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

        public unsafe IAudioControl PlayRT(IAudioStream stream, Func<Vector3> getDirection)
        {
            var al = AlDevice.Current!.Al;

            var buffer = new AlBuffer(al);
            var source = new AlSource(al);

            long curSamples = 0;

            al.GetError();

            var control = new StreamControl();

            buffer.SetCallback(stream.Format, data =>
            {
                if (!stream.IsStreaming || control.IsStopped)
                {
                    _activeStreams.Remove(stream);

                    source.Stop();
                    source.Dispose();

                    buffer.Dispose();

                    return 0;
                }

                int curSize = 0;

                while (curSize < data.Length)
                {
                    var res = stream.Fill(data.Slice(curSize), curSamples / (float)stream.Format.SampleRate);

                    curSamples += res / (stream.Format.BitsPerSample / 8);

                    curSize += res;   
                }

                return curSize; 
            });

            source.SetBuffer(buffer);

            _activeStreams.Add(stream);

            stream.Start();

            source.Play();

            _curSource = source;

            return control;
        }

        public IAudioControl Play(IAudioStream stream, Func<Vector3> getDirection)
        {
            var thread = new Thread(p => PlayWork(stream, getDirection, (StreamControl)p!));
            thread.Name = "Audio Stream Player";
            var control = new StreamControl(thread);
            thread.Start(control);
            return control;
        }

        protected void PlayWork(IAudioStream stream, Func<Vector3> getDirection, StreamControl control)
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

            var source = new AlSource(al);
            source.QueueBuffer(buffers);

            _activeStreams.Add(stream);

            stream.Start();

            source.Play();

            _curSource = source;    

            while (stream.IsStreaming && !control.IsStopped)
            {
                while (source.BuffersProcessed > 0)
                {
                    var buffer = source.DequeueBuffers(1).First();

                    FillBuffer(buffer);

                    source.Direction = getDirection();
                    source.Position = Position ?? _host!.WorldPosition;
                    source.QueueBuffer(buffer);

                    if (source.State == SourceState.Stopped)
                        source.Play();
                }

                if (source.State == SourceState.Stopped)
                    source.Play();

                EngineNativeLib.SleepFor((ulong)(PoolSleepMs * 1000000));
            }

            _activeStreams.Remove(stream);

            source.Stop();
            source.Dispose();

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
