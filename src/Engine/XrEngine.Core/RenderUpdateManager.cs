using System.Collections.Concurrent;

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

        protected struct NotificationStatus
        {
            public bool IsNotifyChangedScene;

            public bool IsDisableNotifyChangedScene;
        }

        protected readonly Scene3D _scene;
        protected long _lastVersion = -1;
        protected readonly List<UpdateGroup> _groups = [];
        protected readonly ConcurrentDictionary<Object3D, NotificationStatus> _notStatus = [];

        public RenderUpdateManager(Scene3D scene)
        {
            _scene = scene;
        }

        public void DisableNotificationsScene(Object3D obj)
        {
            _notStatus.TryAdd(obj, new NotificationStatus
            {
                IsNotifyChangedScene = obj.Is(EngineObjectFlags.NotifyChangedScene),
                IsDisableNotifyChangedScene = obj.Is(EngineObjectFlags.DisableNotifyChangedScene),
            });

            obj.SetFlag(EngineObjectFlags.NotifyChangedScene, false);
            obj.SetFlag(EngineObjectFlags.DisableNotifyChangedScene, true);
        }

        public void RestoreNotificationsScene(Object3D obj)
        {
            if (_notStatus.TryGetValue(obj, out var status))
            {
                obj.SetFlag(EngineObjectFlags.NotifyChangedScene, status.IsNotifyChangedScene);
                obj.SetFlag(EngineObjectFlags.DisableNotifyChangedScene, status.IsDisableNotifyChangedScene);

                _notStatus.TryRemove(obj, out _);
            }
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
                var isParallel = IsParallel;

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
                }
                ;
            }
            finally
            {
                ctx.UpdateOnlySelf = false;
            }
        }

        public bool IsParallel { get; set; }
    }

}
