using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using PhysX.Framework;
using Silk.NET.OpenXR;
using System.Numerics;
using XrEngine.Physics;
using XrMath;

namespace XrEngine.OpenXr
{
    public class OculusHandView : Group3D
    {
        protected XrHandInputMesh? _input;
        protected bool _isInit;

        public OculusHandView()
        {
            CreateRigidBody = false;
        }

        protected override void Start(RenderContext ctx)
        {
            if (XrApp.Current == null)
                throw new ArgumentNullException();

            Name ??= "Hand " + HandType;

            _input = XrApp.Current?.AddHand<XrHandInputMesh>(HandType);

            base.Start(ctx);
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(HandType), HandType);
            container.Write(nameof(CreateRigidBody), CreateRigidBody);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            HandType = container.Read<HandEXT>(nameof(HandType));
            CreateRigidBody = container.Read<bool>(nameof(CreateRigidBody));
        }

        public override T? Feature<T>() where T : class
        {
            if (_input != null && typeof(T).IsAssignableFrom(_input.GetType()))
                return (T)(object)_input;
            return base.Feature<T>();
        }

        protected override void UpdateSelf(RenderContext ctx)
        {
            if (XrApp.Current == null)
                return;


            if (!_isInit && _input != null && _input.IsActive)
            {
                _input.LoadMesh();

                Material capMaterial = (Material)MaterialFactory.CreatePbr(new Color(150 / 255f, 79 / 255f, 72 / 255f));
                Material capMaterial2 = (Material)MaterialFactory.CreatePbr(new Color(100 / 255f, 79 / 255f, 72 / 255f));

                foreach (HandCapsuleFB capsule in _input.Mesh!.Capsules!)
                {
                    Vector3 dir = (capsule.Points.Element1.ToVector3() - capsule.Points.Element0.ToVector3());

                    float len = dir.Length();

                    bool isTip = ((int)capsule.Joint + 1) % 5 == 0;

                    TriangleMesh capMesh = new TriangleMesh(new Capsule3D(capsule.Radius, len), isTip ? capMaterial2 : capMaterial);

                    capMesh.AddComponent(new CapsuleCollider()
                    {
                        Height = len,
                        Radius = capsule.Radius,
                        Mode = CapsuleColliderMode.Top
                    });

                    if (CreateRigidBody)
                    {
                        RigidBody rigidBody = capMesh.AddComponent<RigidBody>();
                        rigidBody.Type = PhysicsActorType.Kinematic;
                        rigidBody.MaterialInfo = new PhysicsMaterialInfo
                        {
                            StaticFriction = 10,
                            Restitution = 0,
                            DynamicFriction = 10
                        };
                    }

                    AddChild(capMesh);
                }
                _isInit = true;
            }

            if (_isInit && _input != null && _input.IsActive)
            {
                for (int i = 0; i < _input.Capsules.Length; i++)
                {
                    HandCapsuleFB capsule = _input.Capsules[i];

                    Object3D mesh = Children[i];

                    Vector3 p0 = capsule.Points.Element0.ToVector3();
                    Vector3 p1 = capsule.Points.Element1.ToVector3();

                    Vector3 dir = (p1 - p0);

                    float h = dir.Length() / _input.Scale;

                    Quaternion orientation = MathUtils.QuatFromForwardUp(dir.Normalize(), new Vector3(0, 1, 0));

                    Vector3 start = p0 + Vector3.Transform(new Vector3(0, 0, -h / 2), orientation);

                    mesh.Transform.Position = start;
                    mesh.Transform.Orientation = orientation;
                    mesh.Transform.SetScale(_input.Scale);
                }
            }

            if (_isInit)
                IsVisible = _input != null && _input.IsActive;

            base.UpdateSelf(ctx);
        }

        public bool CreateRigidBody { get; set; }

        public HandEXT HandType { get; set; }

        public XrHandInputMesh HandInput => _input ?? throw new ArgumentNullException();
    }
}
