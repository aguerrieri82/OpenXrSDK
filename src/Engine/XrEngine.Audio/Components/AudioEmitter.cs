using OpenAl.Framework;
using Silk.NET.OpenAL;
using System.Numerics;
using XrEngine.Media;

namespace XrEngine.Audio
{
    public class AudioEmitter : Behavior<Object3D>, IDisposable
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
                AudioSystem system = _host!.Scene!.Component<AudioSystem>();
                _pool = new AlSourcePool(system.Device.Al);
            }

            _curSource = _pool.Get(buffer);
            _curSource.Position = _host!.WorldPosition;
            _curSource.Direction = direction;

            _curSource.Play();

            return _curSource;
        }

        public IAudioControl PlayRT(IAudioStream stream, Func<Vector3> getDirection)
        {
            AL al = AlDevice.Current!.Al;

            AlBuffer buffer = new AlBuffer(al);
            AlSource source = new AlSource(al);

            long curSamples = 0;

            al.GetError();

            StreamControl control = new StreamControl();

            int SAMPLE_SIZE = stream.Format.BitsPerSample / 8;

            buffer.SetCallback(AudioFormatConverter.ToAlAudioFormat(stream.Format), data =>
            {
                if (!stream.IsStreaming || control.IsStopped)
                {
                    Task.Run(() =>
                    {
                        _activeStreams.Remove(stream);

                        source.Stop();
                        source.Dispose();
                        buffer.Dispose();
                    });

                    return 0;
                }

                int curSize = 0;

                while (curSize < data.Length)
                {
                    int res = stream.Fill(data.Slice(curSize), curSamples / (float)stream.Format.SampleRate);

                    if (res == 0)
                    {
                        control.Stop();
                        return 0;
                    }

                    curSamples += res;

                    curSize += res * SAMPLE_SIZE;
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
            Thread thread = new Thread(p => PlayWork(stream, getDirection, (StreamControl)p!));
            thread.Name = "Audio Stream Player";
            thread.Priority = ThreadPriority.Highest;
            StreamControl control = new StreamControl(thread);
            thread.Start(control);
            return control;
        }

        protected void PlayWork(IAudioStream stream, Func<Vector3> getDirection, StreamControl control)
        {
            AL al = AlDevice.Current!.Al;

            double bufferTime = 0.05f;

            int sampleSize = (stream.Format.BitsPerSample / 8);
            int bufferCount = Math.Max(1, stream.PrefBufferCount);
            int bufferSizeBytes = (int)(bufferTime * stream.Format.SampleRate * sampleSize);

            if (stream.PrefBufferSizeSamples > 0)
                bufferSizeBytes = stream.PrefBufferSizeSamples * sampleSize;

            AlBuffer[] buffers = new AlBuffer[bufferCount];

            byte[] bufferData = new byte[bufferSizeBytes];

            long curSamples = 0;

            void FillBuffer(AlBuffer toFill)
            {
                int totSamples = stream.Fill(bufferData, curSamples / (float)stream.Format.SampleRate);

                toFill.SetData(bufferData, AudioFormatConverter.ToAlAudioFormat(stream.Format));

                curSamples += totSamples;
            }

            for (int i = 0; i < bufferCount; i++)
            {
                buffers[i] = new AlBuffer(al);
                FillBuffer(buffers[i]);
            }

            AlSource source = new AlSource(al);

            source.QueueBuffer(buffers);

            _activeStreams.Add(stream);

            stream.Start();

            source.Play();

            Log.Info(this, "AL Stream Source Play");

            _curSource = source;

            while (stream.IsStreaming && !control.IsStopped)
            {
                while (source.BuffersProcessed > 0)
                {
                    AlBuffer buffer = source.DequeueBuffers(1).First();

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

            foreach (AlBuffer buffer in buffers)
                buffer.Dispose();
        }

        public void Stop()
        {
            if (_activeStreams.Count == 0)
                return;

            foreach (IAudioStream? stream in _activeStreams.ToArray())
                stream.Stop();
        }

        public override void Reset(bool onlySelf = false)
        {
            Stop();
            base.Reset(onlySelf);
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }

        public float PoolSleepMs { get; set; }

        public Vector3? Position { get; set; }

        public AlSource? ActiveSource => _curSource;
    }
}
