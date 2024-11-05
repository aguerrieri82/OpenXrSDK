using PhysX.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using XrEngine;
using XrEngine.Physics;
using XrMath;

namespace XrSamples.Components
{
    public class CarModel : Behavior<Group3D>, IDrawGizmos
    {
        private Group3D? _chassis;
        private Joint? _steerLeft;
        private Joint? _steerRight;
        private float _speed;
        private float _steering;
        private Joint? _rotateLeft;
        private Joint? _rotateRight;

        public void Create()
        {
            Object3D[] wheels = [WheelBL, WheelBR, WheelFR, WheelFL];

            var mat = new PhysicsMaterialInfo
            {
                DynamicFriction = 1f,
                StaticFriction = 1f,
                Restitution = 0.3f
            };

            foreach (var item in wheels)
            {
                var collider = new CylinderCollider();
                collider.Height = item.WorldBounds.Size.X;
                collider.Radius = item.WorldBounds.Size.Y / 2;
                collider.Pose = new Pose3
                {
                    Position = Vector3.Zero,
                    Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2)
                };

                item.AddComponent(collider);

                item.AddComponent(new RigidBody
                {
                    Type = PhysicsActorType.Dynamic,
                    Material = mat,
                    IsEnabled = true,
                    NotCollideGroup = RigidBodyGroup.Group1,
                    AutoTeleport = true,
                    Configure = rb =>
                    {
                        rb.DynamicActor.MaxDepenetrationVelocity = 0f;
                        rb.DynamicActor.MaxAngularVelocity = 2;
                        rb.DynamicActor.MaxLinearVelocity = 1;
                    }

                });

                item.Transform.SetLocalPivot(item.ToLocal(item.WorldBounds.Center), true);
            }

            CreateChassis();

        }

        protected void CreateChassis()
        {
            var manager = _host!.Scene!.Component<PhysicsManager>();
            var size = 0.05f;

            _chassis = new Group3D
            {
                Name = "Chassis"
            };

            var mat = MaterialFactory.CreatePbr("#00ff0040");
            mat.Metalness = 1;
            mat.Alpha = AlphaMode.Blend;

            TriangleMesh AddTube(Vector3 p1, Vector3 p2)
            {
                var line = new Line3(p1, p2);
                var cube = new Cube3D(new Vector3(size, size, line.Length()));
                var mesh = new TriangleMesh(cube, (Material)mat);
                mesh.WorldPosition = line.Center();
                mesh.Forward = line.Direction();
                mesh.AddComponent<BoxCollider>();
                mesh.AddComponent(new RigidBody
                {
                    Type = PhysicsActorType.Dynamic,
                    AutoTeleport = false,
                    NotCollideGroup = RigidBodyGroup.Group1,
                    Configure = rb =>
                    {
                        rb.DynamicActor.MaxDepenetrationVelocity = 0f;
                        rb.DynamicActor.MaxLinearVelocity = 1f;
                        rb.DynamicActor.MaxAngularVelocity = 1f;

                    }
                });

                _chassis.AddChild(mesh);
                return mesh;
            }


            Joint AddRotationLocalAxis(Object3D obj0, Object3D obj1, Vector3 point, Vector3 axis, bool motor = false)
            {
                var q0 = Vector3.UnitX.RotationTowards(axis);
                var q1 = Vector3.UnitX.RotationTowards(axis);
                return AddRotation(obj0, obj1, point, q0, q1, motor);
            }


            Joint AddRotationAxis(Object3D obj0, Object3D obj1, Vector3 point, Line3 axis, bool motor = false)
            {
                var q0 = Vector3.UnitX.RotationTowards(axis.Transform(obj0.WorldMatrixInverse).Direction());
                var q1 = Vector3.UnitX.RotationTowards(axis.Transform(obj1.WorldMatrixInverse).Direction());
                return AddRotation(obj0, obj1, point, q0, q1, motor);
            }


            Joint AddRotation(Object3D obj0, Object3D obj1, Vector3 point, Quaternion q1, Quaternion q2, bool motor = false)
            {
                var p0 = new Pose3
                {
                    Position = obj0.ToLocal(point) - obj0.Transform.LocalPivot,
                    Orientation = q1
                };

                var p1 = new Pose3
                {
                    Position = obj1.ToLocal(point) - obj1.Transform.LocalPivot,
                    Orientation = q2
                };

                var joint = manager.AddJoint(JointType.Revolute, obj0, p0, obj1, p1);

                joint.Configure = _ =>
                {
                    if (motor)
                    {
                        joint.RevoluteJoint.DriveVelocity = 0;
                        joint.RevoluteJoint.RevoluteJointFlags |= PhysX.PxRevoluteJointFlags.DriveEnabled;
                        //joint.RevoluteJoint.ConstraintFlags |= PhysX.PxConstraintFlags.CollisionEnabled;
                    }

                };

                return joint;
            }

            Joint AddFixed(Object3D obj0, Object3D obj1, Vector3 point, Line3 axis)
            {
                var p0 = new Pose3
                {
                    Position = obj0.ToLocal(point) - obj0.Transform.LocalPivot,
                    Orientation = Vector3.UnitX.RotationTowards(axis.Transform(obj0.WorldMatrixInverse).Direction())
                };

                var p1 = new Pose3
                {
                    Position = obj1.ToLocal(point) - obj1.Transform.LocalPivot,
                    Orientation = Vector3.UnitX.RotationTowards(axis.Transform(obj1.WorldMatrixInverse).Direction())
                };

                var joint = manager.AddJoint(JointType.Fixed, obj0, p0, obj1, p1);

                joint.Configure = _ =>
                {
                    joint.FixedJoint.ConstraintFlags |= PhysX.PxConstraintFlags.CollisionEnabled;
                };

                return joint;
            }

            Debug.Assert(WheelFL != null && WheelFR != null && WheelBL != null && WheelBR != null);

            var p1 = WheelFL.WorldBounds.Center;

            var p2 = WheelFR.WorldBounds.Center ;

            var l1 = new Line3(p1, p2);

            var p3 = l1.PointAt(WheelFL.WorldBounds.Size.X / 2);

            var p4 = l1.PointAt(l1.Length() - WheelFL.WorldBounds.Size.X / 2);

            var p5 = l1.Center();

            var p6 = WheelBL.WorldBounds.Center;

            var p7 = WheelBR.WorldBounds.Center;

            var l2 = new Line3(p6, p7);

            var p8 = l2.Center();

            var l3 = new Line3(p5, p8).Expand(-size / 2f, -size / 2f);
            p5 = l3.From;
            p8 = l3.To;

            /*
              t1  |   t3  | t2
             p1--p3--p5--p4--p2
                      |
                      | t5
                      |
              p6-----p8------p7
                     t4
            */

            var t1 = AddTube(p1, p3);
            var t2 = AddTube(p2, p4);
            var t3 = AddTube(p3, p4);
            var t4 = AddTube(p6, p7);
            var t5 = AddTube(p5, p8);

            var q1 = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2);

            _rotateLeft = AddRotationAxis(WheelFL, t1, p1, l1, true);
            _rotateRight = AddRotationAxis(WheelFR, t2, p2, l1, true);

            AddRotationAxis(WheelBL, t4, p6, l2);
            AddRotationAxis(WheelBR, t4, p7, l2);

            AddFixed(t5, t4, p8, l3);
            AddFixed(t5, t3, p5, l3);


            //_steerLeft = AddRotationLocalAxis(t1, t3, p3, Vector3.UnitY);
            //_steerRight = AddRotationLocalAxis(t2, t3, p4, Vector3.UnitY);


            _steerLeft = AddFixed(t1, t3, p3, l1);
            _steerRight = AddFixed(t2, t3, p4, l1);
            /*
            _steerLeft.Pose0 = new Pose3
            {
                Orientation = _steerLeft.Pose0.Orientation,
                Position = _steerLeft.Pose0.Position + new Vector3(0, 0, 0.1f)
            };
            */
            _host!.AddChild(_chassis);
        }

        public void HideCar()
        {
            Debug.Assert(WheelFL != null && WheelFR != null && WheelBL != null && WheelBR != null && SteeringWheel != null && _chassis != null);

            Object3D[] visible = [WheelBL, WheelBR, WheelFR, WheelFL, SteeringWheel, _chassis];

            foreach (var mesh in _host!.DescendantsOrSelf().OfType<TriangleMesh>())
            {
                if (!visible.SelectMany(a => a.DescendantsOrSelf()).Contains(mesh))
                    mesh.IsVisible = false;
            }
        }

        public void DrawGizmos(Canvas3D canvas)
        {

        }

        [Range(-1, 1, 0.01f)]
        public float SteeringValue
        {
            get => _steering;
            set
            {
                _steering = value;

                Debug.Assert(_steerLeft != null && _steerRight != null);

          
                var curLimit = _steerLeft.RevoluteJoint.DriveVelocity;
                curLimit.lower = value;
                curLimit.upper = value;
                _steerLeft.RevoluteJoint.Limit = curLimit;  
            

                /*
                var p0 = _steerLeft.BaseJoint.LocalPose0;

                var rot = Quaternion.CreateFromAxisAngle(Vector3.UnitY, value);
                var axis = Vector3.UnitZ.Transform(rot);

                p0.Orientation = Vector3.UnitX.RotationTowards(axis);

                _steerLeft.BaseJoint.LocalPose0 = p0;
                */

            }
        }

        [Range(0, 10, 0.1f)]
        public float Speed
        {
            get => _speed;
            set
            {
                _speed = value;
                
                Debug.Assert(_rotateLeft != null && _rotateRight != null);

                _rotateLeft.RevoluteJoint.DriveVelocity = value;
                _rotateRight.RevoluteJoint.DriveVelocity = value;
            }
        }


        public Object3D? WheelFL { get; set; }

        public Object3D? WheelFR { get; set; }

        public Object3D? WheelBL { get; set; }

        public Object3D? WheelBR { get; set; }

        public Object3D? SteeringWheel { get; set; }
    }
}
