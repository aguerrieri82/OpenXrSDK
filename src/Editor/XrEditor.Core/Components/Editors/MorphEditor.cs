using System;
using System.Collections.Generic;
using System.Text;
using UI.Binding;
using XrEngine;
using XrEngine.Components;

namespace XrEditor
{
    public class MorphEditor : BaseEditor<MeshMorph, MeshMorph>
    {
        WeightEditor[] _weights;

        public class WeightEditor
        {
            readonly MorphEditor _host;

            public WeightEditor(string? name, int index, MorphEditor host)
            {
                _host = host;

                DisplayName = index.ToString().PadRight(3, ' ') + ".  " + (name ?? "Weigth");
                 
                Name = name;

                var property = new SimpleProperty<float>(
                    () => host.EditValue.Weights[index],
                    value =>
                    {
                        host.EditValue.Weights[index] = value;
                        host.EditValue.InvalidateWeights();
                    }, "Weights " + index);

                Editor = new FloatEditor(property, 0, 1, 0.01f);

                Index = index;
            }

            public FloatEditor Editor { get; }

            public string DisplayName { get;  }

            public string? Name { get; }

            public int Index { get; }
        }


        public MorphEditor()
        {
            _weights = [];
        }


        protected override void OnEditValueChanged(MeshMorph newValue)
        {
            base.OnEditValueChanged(newValue);

            var morphGeo = newValue.Host?.Geometry?.Component<MorphedGeometry>();

            if (morphGeo?.Targets == null)
                return;

            _weights = new WeightEditor[newValue.Weights.Length];

            for (var i = 0; i < newValue.Weights.Length; i++)
                Weights[i] = new WeightEditor(morphGeo.Targets[i].Name, i, this);

            _weights = _weights.OrderBy(a => a.Name).ToArray();

            OnPropertyChanged(nameof(Weights));
        }

        public WeightEditor[] Weights => _weights;
    }
}
