using OpenAl.Framework;
using Silk.NET.OpenAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAl.Framework
{
    public enum AlSourcePoolMode
    {
        Recycle,
        CreateNew
    }

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
            AlSource? result = null;

            if (Mode == AlSourcePoolMode.CreateNew)
            {
                for (var i = _sources.Count - 1; i >= 0; i--)
                {
                    if (_sources[i].State != SourceState.Playing)
                    {
                        _sources[i].Dispose();
                        _sources.RemoveAt(i);
                    }
                }
            }
            else
                result = _sources.FirstOrDefault(a => a.State != SourceState.Playing);

            if (result == null)
            {
                result = new AlSource(_al);
                result.AddBuffer(buffer);
                _sources.Add(result);
            }
            else
            {
                if (result.BufferHandle != buffer.Handle)
                {
                    result.DeleteBuffers();
                    result.AddBuffer(buffer);
                }

                result.Stop();
                result.Rewind();
            }
          
            return result;
        }

        public AlSourcePoolMode Mode { get; set; }
    }
}
