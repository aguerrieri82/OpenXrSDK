using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine.Editor
{
    public class FloatEditor : BaseEditor<float>
    {
        private float _min;
        private float _max;

        public FloatEditor()
        {
            _max = 2f;
            _min = -2f;
        }


        public float Min
        {
            get => _min;
            set
            {
                if (_min == value)
                    return;
                _min = value;
                OnPropertyChanged(nameof(Min));
            }
        }

        public float Max
        {
            get => _max;
            set
            {
                if (_max == value)
                    return;
                _max = value;
                OnPropertyChanged(nameof(Max));
            }
        }
    }
}
