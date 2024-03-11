using OpenAl.Framework;
using Silk.NET.OpenAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAl.Framework
{
    public class AlSourcePool
    {
        protected readonly List<AlSource> _sources = [];
        protected readonly AL _al;

        public AlSourcePool(AL al)
        {
            _al = al;
        }

        public AlSource Get(AlBuffer buffer)
        {
            for (var i = _sources.Count -1; i>= 0; i--)
            {
                if (_sources[i].State != SourceState.Playing)
                {
                    _sources[i].Dispose();
                    _sources.RemoveAt(i);
                }
            }

            //var result = _sources.FirstOrDefault(a=> a.State != SourceState.Playing);

            AlSource? result = null;

            if (result == null)
            {
                result = new AlSource(_al);
                _sources.Add(result);
            }
            else
            {
                result.DeleteBuffers();
                result.Stop();
                result.Rewind();
            }
          
            result.AddBuffer(buffer);

            return result;
        }
    }
}
