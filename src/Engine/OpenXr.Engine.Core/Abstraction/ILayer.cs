using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public interface ILayer : IObjectChangeListener
    {

        void Attach(LayerManager manager);

        void Detach();

        IEnumerable<ILayerObject> Content { get; }

        bool IsVisible { get; set; }

        ObjectId Id { get; }
    }
}
