using PhysX.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrEngine.Physics;
using XrMath;

namespace XrEngine.OpenXr
{
    public delegate Object3D CreateModelDelegate(SceneModelInfo info);

    public class SceneModelOptions
    {
        public Material? Material { get; set; }

        public bool AddPhysics { get; set; }

        public CreateModelDelegate? CreateModel { get; set; }
    }


    public class DefaultSceneModelFactory : ISceneModelFactory
    {
        readonly Dictionary<SceneModelType, SceneModelOptions> _models = [];

        protected DefaultSceneModelFactory()
        {

        }

        public Object3D? CreateModel(SceneModelInfo model)
        {
            if (!_models.TryGetValue(model.Type, out var options))
                return null;

            var obj = options.CreateModel?.Invoke(model);

            if (obj == null)
                return null;

            if (string.IsNullOrEmpty(obj.Name))
                obj.Name = model.Type.ToString();

            if (options.AddPhysics)
            {
                if (!obj.Components<ICollider3D>().Any())
                    obj.AddComponent<BoxCollider>();

                AddRigidBody(obj);
            }

            obj.WorldPosition = model.Pose.Position;
            obj.WorldOrientation = model.Pose.Orientation;

            obj.SetProp("SceneModel", model);

            return obj;
        }

        public virtual Object3D CreateCube(SceneModelInfo model, SceneModelOptions options)
        {
            var material = options.Material ?? CreateMaterial();

            var obj = new TriangleMesh(
                new Cube3D(new Vector3(model.Size.X, model.Size.Y, 0.01f)),
                material);

            obj.Geometry!.ScaleUV(new Vector2(model.Size.X, model.Size.Y));
            obj.RenderPriority = -1;

            if (model.Type == SceneModelType.Wall)
            {
                material.CompareStencilMask = 2;
                material.StencilFunction = StencilFunction.NotEqual;
            }

            return obj;
        }

        public virtual Object3D CreateMesh(SceneModelInfo model, SceneModelOptions options)
        {
            var material = options.Material ?? CreateMaterial();

            var obj = new TriangleMesh(model.Geometry!, material);

            if (options.AddPhysics)
                obj.AddComponent(new PyMeshCollider());

            return obj;
        }

        protected virtual Material CreateMaterial()
        {
            var mat = MaterialFactory.CreatePbr(Color.White);
            mat.Roughness = 0.8f;
            mat.Metalness = 0;
            mat.ShadowColor = new Color(0, 0, 0, 0.7f);

            return (Material)mat;
        }

        protected RigidBody AddRigidBody(Object3D obj)
        {
            var rigidBody = obj.AddComponent<RigidBody>();

            rigidBody.Type = PhysicsActorType.Static;
            rigidBody.ContactOffset = 0.2f;
            rigidBody.Material = new PhysicsMaterialInfo
            {
                DynamicFriction = 1,
                StaticFriction = 1,
                Restitution = 0.7f
            };

            return rigidBody;   
        }

        public void AddMesh(Material? material = null, bool addPhysics = false)
        {
            var options = new SceneModelOptions
            {
                Material = material,
                AddPhysics = addPhysics,
            };

            options.CreateModel = model => CreateMesh(model, options);

            _models[SceneModelType.Mesh] = options;
        }

        public void AddWalls(Material? material = null, bool addPhysics = false)
        {
            var options = new SceneModelOptions
            {
                Material = material,
                AddPhysics = addPhysics
            };

            options.CreateModel = model => CreateCube(model, options);

            _models[SceneModelType.Ceiling] = options;
            _models[SceneModelType.Floor] = options;
            _models[SceneModelType.Wall] = options;
        }

        public void Add(SceneModelType type, CreateModelDelegate factory, bool addPhysics = false)
        {
            var options = new SceneModelOptions
            {
                AddPhysics = addPhysics,
                CreateModel = model => factory(model)
            };
            _models[type] = options;
        }

        public Dictionary<SceneModelType, SceneModelOptions> Models => _models;


        public static readonly DefaultSceneModelFactory Instance = new DefaultSceneModelFactory();   
    }
}
