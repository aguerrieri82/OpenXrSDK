namespace OpenXr.Engine
{
    public static class EngineExtensions
    {
        #region EngineObject

        public static Behavior<T> AddBehavior<T>(this T obj, Action<T, RenderContext> action) where T : EngineObject
        {
            var result = new LambdaBehavior<T>(action);
            obj.AddComponent(result);
            return result;
        }

        #endregion

        #region OBJECT3D

        public static IEnumerable<Group> Ancestors(this Object3D obj)
        {
            var curItem = obj.Parent;

            while (curItem != null)
            {
                yield return curItem;
                curItem = curItem.Parent;
            }
        }

        public static T? FindAncestor<T>(this Object3D obj) where T : Group
        {
            return obj.Ancestors().OfType<T>().FirstOrDefault();
        }

        #endregion

        #region SCENE

        public static T AddLayer<T>(this Scene scene) where T : ILayer, new()
        {
            return scene.AddLayer(new T());
        }

        public static T AddLayer<T>(this Scene scene, T layer) where T : ILayer
        {
            scene.Layers.Add(layer);
            return layer;
        }

        public static IEnumerable<T> TypeLayerContent<T>(this Scene scene) where T: Object3D
        {
            var layer = scene.Layers.OfType<TypeLayer<T>>().FirstOrDefault();
            if (layer == null)
                return [];
            return layer.Content.Cast<T>();
        }

        #endregion

        #region GROUP

        public static IEnumerable<T> VisibleDescendants<T>(this Group target) where T : Object3D
        {
            return target.Descendants<T>().Where(a => a.IsVisible);
        }

        public static IEnumerable<Object3D> Descendants(this Group target)
        {
            return target.Descendants<Object3D>();
        }

        public static IEnumerable<T> Descendants<T>(this Group target) where T : Object3D
        {
            foreach (var child in target.Children)
            {
                if (child is T validChild)
                    yield return validChild;

                if (child is Group group)
                {
                    foreach (var desc in group.Descendants<T>())
                        yield return desc;
                }
            }
        }

        #endregion

        #region ENGINE APP

        public static void OpenScene(this EngineApp app, string name)
        {
            app.OpenScene(app.Scenes.Single(s => s.Name == name));
        }

        #endregion

        #region MISC

        public static void Update<T>(this IEnumerable<T> target, RenderContext ctx) where T : IRenderUpdate
        {
            target.ForeachSafe(a => a.Update(ctx));
        }

        #endregion
    }
}
