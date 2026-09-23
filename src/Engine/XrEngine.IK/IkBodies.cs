using System.Numerics;

namespace XrEngine.IK
{
    public static class IkBodies
    {
        public sealed class Body
        {
            readonly Dictionary<string, int> _boneMap;

            internal Body(IkBone[] bones, string[] names, float[] sizes, Dictionary<string, int> boneMap)
            {
                Bones = bones;
                Names = names;
                Sizes = sizes;
                _boneMap = boneMap;
            }

            public int this[string name] => _boneMap[name];

            public bool TryGetBone(string name, out int index) =>
                _boneMap.TryGetValue(name, out index);

            public IkBone[] Bones { get; }

            public string[] Names { get; }

            public float[] Sizes { get; }
        }

        public static Body CreateArms()
        {
            const float HEIGHT = 1.72f;

            const float PELVIS_HEIGHT = 0.90f;

            const float SPINE_LOWER = 0.18f;
            const float SPINE_UPPER = 0.18f;
            const float NECK_HEAD = 0.22f;

            const float SHOULDER_HALF_WIDTH = 0.19f;

            const float UPPER_ARM_LENGTH = 0.30f;
            const float FOREARM_LENGTH = 0.26f;
            const float HAND_LENGTH = 0.10f;

            const float JOINT_SIZE = 0.10f;
            const float EFFECTOR_SIZE = 0.01f;

            var bones = new List<IkBone>();
            var names = new List<string>();
            var sizes = new List<float>();
            var boneMap = new Dictionary<string, int>();

            int Add(string name, float size, int parent, Vector3 offset, IkAxis axis,
                float min = 0, float max = 0, float rest = 0)
            {
                var index = bones.Count;

                var minimum = Vector3.Zero;
                var maximum = Vector3.Zero;

                if ((axis & IkAxis.X) != 0)
                {
                    minimum.X = Rad(min);
                    maximum.X = Rad(max);
                }

                if ((axis & IkAxis.Y) != 0)
                {
                    minimum.Y = Rad(min);
                    maximum.Y = Rad(max);
                }

                if ((axis & IkAxis.Z) != 0)
                {
                    minimum.Z = Rad(min);
                    maximum.Z = Rad(max);
                }

                var initialRotation = axis switch
                {
                    IkAxis.X => Quaternion.CreateFromAxisAngle(Vector3.UnitX, Rad(rest)),
                    IkAxis.Y => Quaternion.CreateFromAxisAngle(Vector3.UnitY, Rad(rest)),
                    IkAxis.Z => Quaternion.CreateFromAxisAngle(Vector3.UnitZ, Rad(rest)),
                    _ => Quaternion.Identity
                };

                bones.Add(new IkBone
                {
                    Parent = parent,
                    Axes = axis,
                    Offset = offset,
                    RestOrientation = Quaternion.Identity,
                    InitialRotation = initialRotation,
                    Length = 0,
                    LimitedAxes = axis,
                    Minimum = minimum,
                    Maximum = maximum,
                    Stiffness = Vector3.Zero
                });

                names.Add(name);
                sizes.Add(size);
                boneMap[name] = index;

                return index;
            }

            // Torso

            var pelvis = Add(
                "Pelvis", JOINT_SIZE,
                -1,
                new Vector3(0, PELVIS_HEIGHT, 0),
                IkAxis.Y,
                -30, 30);

            var spine1 = Add(
                "Spine1", JOINT_SIZE,
                pelvis,
                new Vector3(0, SPINE_LOWER, 0),
                IkAxis.X,
                -10, 10);

            var spine2 = Add(
                "Spine2", JOINT_SIZE,
                spine1,
                new Vector3(0, SPINE_UPPER, 0),
                IkAxis.X,
                -10, 10);

            // rotational joint at approximately chest/shoulder level
            var spine3 = Add(
                "Spine3", JOINT_SIZE,
                spine2,
                Vector3.Zero,
                IkAxis.Y,
                -20, 20);

            // Head target approximately at head centre
            Add(
                "Head", EFFECTOR_SIZE,
                spine3,
                new Vector3(0, NECK_HEAD, 0),
                IkAxis.None);

            // Left arm

            var lShoulder = Add(
                "Shoulder-L", JOINT_SIZE,
                spine3,
                new Vector3(-SHOULDER_HALF_WIDTH, 0, 0),
                IkAxis.Y,
                -90, 0);

            var lShoulderPitch = Add(
                "Shoulder-Pitch-L", JOINT_SIZE,
                lShoulder,
                Vector3.Zero,
                IkAxis.Z,
                -90, 90);

            var lUpArm = Add(
                "UpArm-L", JOINT_SIZE,
                lShoulderPitch,
                new Vector3(-UPPER_ARM_LENGTH, 0, 0),
                IkAxis.Y,
                -90, 0);

            var lLowArm = Add(
                "LowArm-L", JOINT_SIZE,
                lUpArm,
                new Vector3(-FOREARM_LENGTH, 0, 0),
                IkAxis.Y,
                -90, 30);

            Add(
                "Hand-L", EFFECTOR_SIZE,
                lLowArm,
                new Vector3(-HAND_LENGTH, 0, 0),
                IkAxis.None);

            // Right arm

            var rShoulder = Add(
                "Shoulder-R", JOINT_SIZE,
                spine3,
                new Vector3(SHOULDER_HALF_WIDTH, 0, 0),
                IkAxis.Y,
                0, 90);

            var rShoulderPitch = Add(
                "Shoulder-Pitch-R", JOINT_SIZE,
                rShoulder,
                Vector3.Zero,
                IkAxis.Z,
                -90, 90);

            var rUpArm = Add(
                "UpArm-R", JOINT_SIZE,
                rShoulderPitch,
                new Vector3(UPPER_ARM_LENGTH, 0, 0),
                IkAxis.Y,
                0, 90);

            var rLowArm = Add(
                "LowArm-R", JOINT_SIZE,
                rUpArm,
                new Vector3(FOREARM_LENGTH, 0, 0),
                IkAxis.Y,
                -30, 90);

            Add(
                "Hand-R", EFFECTOR_SIZE,
                rLowArm,
                new Vector3(HAND_LENGTH, 0, 0),
                IkAxis.None);

            return new Body(
                bones.ToArray(),
                names.ToArray(),
                sizes.ToArray(),
                boneMap);
        }

        static float Rad(float value) =>
            value * MathF.PI / 180f;
    }
}