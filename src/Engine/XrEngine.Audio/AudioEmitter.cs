using OpenAl.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Audio
{
    public class AudioEmitter : Behavior<Object3D>
    {
        static AlSourcePool? _pool;

        public void Play(AlBuffer buffer, Vector3 direction)
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
        }
    }
}
