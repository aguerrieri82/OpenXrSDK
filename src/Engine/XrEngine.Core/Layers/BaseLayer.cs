using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Layers
{
    public abstract class BaseLayer<T> : ILayer3D where T : ILayer3DItem
    {
        protected readonly ObjectId _id;
        protected readonly HashSet<T> _content = [];
        protected LayerManager? _manager;
        protected long _version;

        public BaseLayer()
        {
            _id = ObjectId.New();
            Name = GetType().Name;  
        }

        public void Attach(LayerManager manager)
        {
            _manager = manager;


        }

        public virtual void Detach()
        {
            _manager = null;
        }

        public virtual void NotifyChanged(Object3D object3D, ObjectChange change)
        {

        }

        public bool IsVisible { get; set; }

        public ObjectId Id => _id;

        public long Version => _version;

        public string Name { get; set; }

        public IEnumerable<ILayer3DItem> Content => (IEnumerable<ILayer3DItem>)_content;

    }
}
