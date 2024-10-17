using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrMath
{
    public struct BoundsBuilder
    {
        Bounds3 _result;
        bool _isEmpty;

        public BoundsBuilder()
        {
            _result.Min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            _result.Max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            _isEmpty = true;    
        }

        public void Add(Vector3 point)
        {
            _result.Min = Vector3.Min(_result.Min, point);
            _result.Max = Vector3.Max(_result.Max, point);
            _isEmpty = false;   
        }

        public void Add(IEnumerable<Vector3> points)
        {
            foreach (var point in points)
                Add(point);
        }

        public void Add(Bounds3 newBounds)
        {
            _result.Min = Vector3.Min(newBounds.Min, _result.Min);
            _result.Max = Vector3.Max(newBounds.Max, _result.Max);
            _isEmpty = false;
        }

        public void Add(IEnumerable<Bounds3> newBounds)
        {
            foreach (var item in newBounds)
                Add(item);
        }

        public readonly Bounds3 Result => _isEmpty ? Bounds3.Zero : _result; 
    }
}
