﻿using OpenAl.Framework;
using Silk.NET.OpenAL;

namespace XrEngine.Audio
{
    public class DynamicSound
    {
        readonly List<Tuple<int, AlBuffer>> _buffers = [];
        int _maxVel;
        int _minVel;

        public DynamicSound()
        {
            _maxVel = int.MinValue;
            _minVel = int.MaxValue;
        }

        public void AddBuffers(AL al, IAssetStore assetStore, string dirPath)
        {

            foreach (var file in assetStore.List(dirPath))
                AddBuffer(al, assetStore, file);

            Commit();
        }

        public void AddBuffer(AL al, IAssetStore assetStore, string filePath, int? velocity = null)
        {
            if (velocity == null)
            {
                var name = Path.GetFileNameWithoutExtension(filePath);
                if (!int.TryParse(name, out var value))
                    return;
                velocity = value;
            }

            var ext = Path.GetExtension(filePath);

            if (ext == ".wav")
            {
                var reader = new WavReader();
                using var stream = assetStore.Open(filePath);
                var data = reader.Decode(stream);
                var buffer = new AlBuffer(al);
                buffer.SetData(data);
                AddBuffer(velocity.Value, buffer);
                return;
            }

            throw new NotSupportedException();
        }


        public void AddBuffer(int velocity, AlBuffer buffer)
        {
            _minVel = Math.Min(velocity, _minVel);
            _maxVel = Math.Max(velocity, _maxVel);
            _buffers.Add(new Tuple<int, AlBuffer>(velocity, buffer));
        }

        public void Commit()
        {
            _buffers.Sort((a, b) => a.Item1 - b.Item1);
        }

        public AlBuffer Buffer(float velocity)
        {
            var targetVel = (int)Math.Round(_minVel + (_maxVel - _minVel) * velocity);

            int curI = 0;

            while (true)
            {
                if (curI == 0 && targetVel < _buffers[curI].Item1)
                    break;

                if (curI < _buffers.Count - 1 && targetVel >= _buffers[curI].Item1 && targetVel < _buffers[curI + 1].Item1)
                    break;

                if (curI == _buffers.Count - 1)
                    break;

                curI++;
            }

            return _buffers[curI].Item2;

        }
    }
}
