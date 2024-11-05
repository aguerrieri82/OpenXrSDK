using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrMath
{
    public struct CubeFaces : IEnumerable<Quad3>
    {
        public Quad3 Top;

        public Quad3 Bottom;

        public Quad3 Left;

        public Quad3 Right;

        public Quad3 Front;

        public Quad3 Back;

        public IEnumerator<Quad3> GetEnumerator()
        {
            yield return Top;
            yield return Bottom;
            yield return Left;
            yield return Right;
            yield return Front;
            yield return Back;

        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
