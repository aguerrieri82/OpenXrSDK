using XrEngine.Services;

namespace XrEngine
{
    public class EngineObjectStateManager : StateObjectManager<EngineObject>
    {
        EngineObjectStateManager() { }

        public override EngineObject? Read(string key, EngineObject? destObj, Type objType, IStateContainer container)
        {
            if (destObj != null && destObj.Is(EngineObjectFlags.Readonly) && !container.Is(StateContextFlags.Store))
                return destObj;

            var objState = container.Enter(key, true);

            if (objState == null)
            {
                if (container.Context.Is(StateContextFlags.Update))
                    return destObj;
                return null;
            }

            if (objState.Contains("$uri"))
            {
                var assetUri = objState.Read<Uri>("$uri");

                bool mustLoad = true;

                if (destObj != null)
                {
                    var curAsset = destObj.Component<AssetSource>();
                    if (curAsset != null && curAsset.Asset?.Source == assetUri)
                        mustLoad = false;
                }

                if (mustLoad)
                    destObj = AssetLoader.Instance.Load(assetUri, objType, destObj);

                return base.Read("Id", destObj, objType, objState);
            }

            return base.Read(key, destObj, objType, container);
        }

        public override void Write(string key, EngineObject? obj, IStateContainer container)
        {
            if (obj == null)
                return;

            if (obj.Is(EngineObjectFlags.Generated))
                return;

            obj.EnsureId();

            if (obj.Is(EngineObjectFlags.Readonly) && !container.Is(StateContextFlags.Store))
            {
                ObjectIdStateManager.Instance.Write(key, obj.Id, container);
                return;
            }

            var objState = container.Enter(key);

            if (obj.TryComponent<AssetSource>(out var assetSrc))
            {
                objState.Write("$uri", assetSrc.Asset!.Source);
                base.Write("Id", obj, objState);
            }
            else
                base.Write(key, obj, container);
        }

        public static readonly EngineObjectStateManager Instance = new();
    }
}
