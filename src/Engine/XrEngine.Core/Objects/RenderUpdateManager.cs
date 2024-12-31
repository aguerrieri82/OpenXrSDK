namespace XrEngine
{
    public class RenderUpdateManager
    {
        protected class UpdateGroup
        {
            public int Priority;

            public string? Name;

            public bool IsParallel;

            public IList<IRenderUpdate> Items = [];
        }

        protected readonly Scene3D _scene;
        protected long _lastVersion = -1;
        protected readonly List<UpdateGroup> _groups = [];

        public RenderUpdateManager(Scene3D scene)
        {
            _scene = scene;
        }

        protected void Build()
        {
            var objGroup = new UpdateGroup()
            {
                Name = "Objects",
                IsParallel = false,
                Priority = 0
            };

            var leafGroup = new UpdateGroup()
            {
                Name = "Leafs",
                IsParallel = true,
                Priority = 1
            };

            _groups.Clear();
            _groups.AddRange(objGroup, leafGroup);

            void Visit(object obj)
            {
                if (obj is EngineObject engObj)
                {
                    foreach (var comp in engObj.Components<IComponent>().OfType<IRenderUpdate>())
                    {
                        var priority = comp.UpdatePriority + 100;
                        var compGroup = _groups.FirstOrDefault(a => a.Priority == priority);
                        if (compGroup == null)
                        {
                            compGroup = new UpdateGroup()
                            {
                                Priority = priority,
                                IsParallel = true,
                                Name = "Components " + comp.UpdatePriority
                            };

                            _groups.Add(compGroup);
                        }
                        compGroup.Items.Add(comp);
                    }

                    if (obj is Group3D grp)
                    {
                        if (obj is not Scene3D)
                            objGroup.Items.Add(grp);
                        foreach (var child in grp.Children)
                            Visit(child);
                    }
                    else if (obj is Object3D obj3d)
                        leafGroup.Items.Add(obj3d);
                }
            }

            Visit(_scene);

            _groups.Sort((a, b) => a.Priority - b.Priority);

            _lastVersion = _scene.ContentVersion;
        }

        public void Update(RenderContext ctx)
        {
            ctx.UpdateOnlySelf = true;

            try
            {
                bool isParallel = false;

                if (_scene.ContentVersion != _lastVersion)
                {
                    Build();
                    isParallel = false;
                }

                foreach (var grp in _groups)
                {
                    if (grp.IsParallel && isParallel)
                        Parallel.ForEach(grp.Items, item => item.Update(ctx));
                    else
                    {
                        foreach (var item in grp.Items)
                            item.Update(ctx);
                    }
                };
            }
            finally
            {
                ctx.UpdateOnlySelf = false;
            }
        }
    }
}
