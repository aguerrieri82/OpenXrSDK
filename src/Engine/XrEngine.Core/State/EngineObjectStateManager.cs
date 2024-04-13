using XrEngine.Services;

namespace XrEngine
{
    public class EngineObjectStateManager : ITypeStateManager<EngineObject?>
    {
        EngineObjectStateManager() { }

        public EngineObject? Read(string key, EngineObject? destObj, Type objType, IStateContainer container)
        {
            if (destObj != null && destObj.Is(EngineObjectFlags.Readonly) && !container.Is(StateContextFlags.Store))
                return destObj;

            var objState = container.Enter(key, true);

            if (objState == null)
                return null;

            if (objState.Contains("$uri"))
            {
                var assetUri = objState.Read<Uri>("$uri");
                return AssetLoader.Instance.Load(assetUri, objType, destObj);
            }

            return (EngineObject?)StateObjectManager.Instance.Read(key, destObj, objType, container);
        }

        public void Write(string key, EngineObject? obj, IStateContainer container)
        {
            if (obj == null)
                return;
            
            obj.EnsureId();

            if (obj.Is(EngineObjectFlags.Readonly) && !container.Is(StateContextFlags.Store))
            {

                ObjectIdStateManager.Instance.Write(key, obj.Id, container);
                return;
            }

            var objState = container.Enter(key);

            var assetSrc = obj!.Components<AssetSource>().FirstOrDefault();
            if (assetSrc != null)
            {
                objState.Write("$uri", assetSrc.AssetUri);
                objState.Write("Id", obj.Id);
            }
            else
                StateObjectManager.Instance.Write(key, obj, container);
        }

        public static readonly EngineObjectStateManager Instance = new();
    }
}
