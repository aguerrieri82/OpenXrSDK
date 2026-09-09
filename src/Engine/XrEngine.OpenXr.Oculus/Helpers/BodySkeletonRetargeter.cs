using OpenXr.Framework;
using Silk.NET.OpenXR;
using System.Numerics;
using XrMath;

namespace XrEngine.OpenXr.Oculus
{
    /// <summary>
    /// Rebuilds the weighted avatar joints on the full XR hierarchy.
    /// Geometry fitting is absorbed into inverse binds; tracked joint frames remain exactly XR.
    /// </summary>
    [AIGenerated]
    public sealed class BodySkeletonRetargeter
    {
        public readonly record struct SkinBindingCorrection(FullBodyJointMETA ReferenceJoint, Matrix4x4 Transform);

        private Dictionary<FullBodyJointMETA, SkinBindingCorrection> _skinCorrections = new();
        private Dictionary<Joint3D, Matrix4x4>? _fittedBind;
        private Dictionary<Joint3D, Matrix4x4>? _jointBind;
        private Matrix4x4[] _trackingBind = [];
        private BodySkeletonJointFB[] _sourceRest = [];

        private readonly Joint3D?[] _mapped;
        private readonly Joint3D[] _joints;
        private readonly int _rootIndex;
        private readonly Group3D _container;

        private readonly Dictionary<Joint3D, Matrix4x4> _originalWorld;
        private readonly Dictionary<Joint3D, Matrix4x4> _originalLocal;
        private readonly Dictionary<Joint3D, Joint3D?> _originalParents;
        private readonly Joint3D[] _originalOrder;
        private readonly (MeshSkin Skin, Matrix4x4[] Bind)[] _skins;
        private readonly Dictionary<Joint3D, int> _indices;

        private readonly Vector3[] _positions;
        private readonly Quaternion[] _orientations;

        private int[] _order = [];


        public BodySkeletonRetargeter(BodySkeletonJointFB[] sourceRest,
            Joint3D?[] targetJoints, int rootIndex, MeshSkin[] skins,
            IReadOnlyDictionary<FullBodyJointMETA, SkinBindingCorrection>? skinCorrections = null)
        {
            if (rootIndex < 0 || rootIndex >= targetJoints.Length || targetJoints[rootIndex] == null)
                throw new ArgumentException("The avatar must have a mapped root joint.", nameof(targetJoints));

            _rootIndex = rootIndex;
            _mapped = (Joint3D?[])targetJoints.Clone();

            var root = _mapped[rootIndex]!;

            _container = root.Parent ?? throw new ArgumentException("The avatar root must have a container.");

            _originalOrder = new[] { root }
                .Concat(root.Descendants().OfType<Joint3D>())
                .ToArray();

            _originalWorld = _originalOrder.ToDictionary(j => j, j => j.WorldMatrix);
            _originalLocal = _originalOrder.ToDictionary(j => j, j => j.Transform.Matrix);
            _originalParents = _originalOrder.ToDictionary(j => j, j => j.Parent as Joint3D);

            ValidateAvatarHierarchy(root);

            _indices = CreateJointIndices();

            _skins = skins.Select(s =>
            {
                if (s.Joints == null || s.InverseBindMatrices == null || s.Joints.Length != s.InverseBindMatrices.Length)
                    throw new ArgumentException("Skin joints and inverse binds must have equal lengths.", nameof(skins));

                return (s, (Matrix4x4[])s.InverseBindMatrices.Clone());

            }).ToArray();

            _joints = new Joint3D[targetJoints.Length];
            _positions = new Vector3[targetJoints.Length];
            _orientations = new Quaternion[targetJoints.Length];

            _skinCorrections = ValidateCorrections(skinCorrections);

            Rebind(sourceRest);
        }


        public bool IsBoundTo(BodySkeletonJointFB[] skeleton)
            => ReferenceEquals(_sourceRest, skeleton);


        public void SetSkinCorrections(IReadOnlyDictionary<FullBodyJointMETA, SkinBindingCorrection>? corrections)
        {
            var copy = ValidateCorrections(corrections);

            if (_fittedBind != null && _jointBind != null)
            {
                var binds = CreateSkinBinds(_fittedBind, _jointBind, _trackingBind, copy);

                for (var i = 0; i < _skins.Length; i++)
                    _skins[i].Skin.InverseBindMatrices = binds[i];
            }

            _skinCorrections = copy;
        }


        public void Rebind(BodySkeletonJointFB[] sourceRest)
        {
            if (sourceRest.Length != _mapped.Length)
                throw new ArgumentException("XR skeleton and mapping lengths differ.", nameof(sourceRest));

            var order = ValidateSkeleton(sourceRest);
            var trackingRest = CreateTrackingRest(sourceRest);
            var fittedBind = Fit(trackingRest);
            var jointBind = CreateJointBind(fittedBind, trackingRest);
            var skinBinds = CreateSkinBinds(fittedBind, jointBind, trackingRest, _skinCorrections);

            RebuildHierarchy(sourceRest, order, trackingRest, jointBind);

            for (var i = 0; i < _skins.Length; i++)
                _skins[i].Skin.InverseBindMatrices = skinBinds[i];

            _order = order;
            _sourceRest = sourceRest;
            _fittedBind = fittedBind;
            _jointBind = jointBind;
            _trackingBind = trackingRest;
        }


        public void Update(BodyJointLocationFB[] locations, in Matrix4x4 baseTransform)
        {
            if (locations.Length != _joints.Length)
                throw new ArgumentException("Invalid tracking frame or root offset.");

            foreach (var i in _order)
            {
                var location = locations[i];
                var pose = location.Pose.ToPose3();

                if ((location.LocationFlags & SpaceLocationFlags.PositionValidBit) != 0 && Valid(pose.Position))
                    _positions[i] = pose.Position;

                if ((location.LocationFlags & SpaceLocationFlags.OrientationValidBit) != 0 && Valid(pose.Orientation))
                    _orientations[i] = Quaternion.Normalize(pose.Orientation);

                _joints[i].WorldMatrix = Pose(_positions[i], _orientations[i]) * baseTransform;
            }
        }


        private void ValidateAvatarHierarchy(Joint3D root)
        {
            foreach (var joint in _originalOrder)
            {
                if (joint == root)
                    continue;

                var parent = _originalParents[joint];

                if (parent == null || !_originalWorld.ContainsKey(parent))
                    throw new ArgumentException("Avatar bones must form a joint hierarchy without intermediate non-joint groups.");
            }
        }


        private Dictionary<Joint3D, int> CreateJointIndices()
        {
            var result = new Dictionary<Joint3D, int>();

            for (var i = 0; i < _mapped.Length; i++)
            {
                var joint = _mapped[i];

                if (joint == null)
                    continue;

                if (!_originalWorld.ContainsKey(joint))
                    throw new ArgumentException("Mapped joints must be descendants of the root.");

                if (!result.TryAdd(joint, i))
                    throw new ArgumentException("Mapped joints must be distinct.");
            }

            return result;
        }


        private int[] ValidateSkeleton(BodySkeletonJointFB[] sourceRest)
        {
            var order = new List<int>();
            var visited = new byte[sourceRest.Length];

            void Visit(int index)
            {
                if (visited[index] == 2)
                    return;

                if (visited[index] == 1)
                    throw new ArgumentException("XR skeleton contains a cycle.");

                visited[index] = 1;

                var parent = sourceRest[index].ParentJoint;

                if (parent < -1 || parent >= sourceRest.Length)
                    throw new ArgumentException("Invalid XR parent joint.");

                if ((parent == -1) != (index == _rootIndex))
                    throw new ArgumentException("Invalid XR root joint.");

                if (parent >= 0)
                    Visit(parent);

                visited[index] = 2;
                order.Add(index);
            }

            for (var i = 0; i < sourceRest.Length; i++)
                Visit(i);

            return order.ToArray();
        }


        private static Matrix4x4[] CreateTrackingRest(BodySkeletonJointFB[] sourceRest)
        {
            var result = new Matrix4x4[sourceRest.Length];

            for (var i = 0; i < sourceRest.Length; i++)
            {
                var pose = sourceRest[i].Pose.ToPose3();

                if (!Valid(pose.Position) || !Valid(pose.Orientation))
                    throw new ArgumentException("XR bind pose is invalid.");

                result[i] = Pose(pose.Position, Quaternion.Normalize(pose.Orientation));
            }

            return result;
        }


        private Dictionary<Joint3D, Matrix4x4> CreateJointBind(
            Dictionary<Joint3D, Matrix4x4> fittedBind,
            Matrix4x4[] trackingRest)
        {
            var result = new Dictionary<Joint3D, Matrix4x4>();

            foreach (var joint in _originalOrder)
            {
                if (_indices.TryGetValue(joint, out var index))
                {
                    result[joint] = trackingRest[index];
                    continue;
                }

                var fitted = fittedBind[joint];

                if (Matrix4x4.Decompose(fitted, out _, out var rotation, out var position))
                {
                    result[joint] = Pose(position, Quaternion.Normalize(rotation));
                    continue;
                }

                rotation = Quaternion.CreateFromRotationMatrix(_originalWorld[joint]);
                result[joint] = Pose(fitted.Translation, Quaternion.Normalize(rotation));
            }

            return result;
        }


        private void RebuildHierarchy(BodySkeletonJointFB[] sourceRest, int[] order,
            Matrix4x4[] trackingRest, Dictionary<Joint3D, Matrix4x4> jointBind)
        {
            for (var i = 0; i < _joints.Length; i++)
            {
                _joints[i] ??= _mapped[i] ?? new Joint3D { Name = "XR_" + (FullBodyJointMETA)i };
                _joints[i].Parent?.RemoveChild(_joints[i]);
            }

            foreach (var i in order)
            {
                var joint = _joints[i];
                var parent = sourceRest[i].ParentJoint;

                (parent < 0 ? _container : _joints[parent]).AddChild(joint);

                joint.Transform.LocalPivot = Vector3.Zero;
                joint.WorldMatrix = trackingRest[i];

                _positions[i] = trackingRest[i].Translation;
                _orientations[i] = Quaternion.CreateFromRotationMatrix(trackingRest[i]);
            }

            foreach (var joint in _originalOrder)
            {
                if (!_indices.ContainsKey(joint))
                    joint.WorldMatrix = jointBind[joint];
            }
        }


        private Dictionary<FullBodyJointMETA, SkinBindingCorrection> ValidateCorrections(
            IReadOnlyDictionary<FullBodyJointMETA, SkinBindingCorrection>? corrections)
        {
            var result = new Dictionary<FullBodyJointMETA, SkinBindingCorrection>();

            if (corrections == null)
                return result;

            foreach (var (id, correction) in corrections)
            {
                ValidateCorrectionTarget(id);
                ValidateCorrectionReference(correction.ReferenceJoint);
                ValidateCorrectionTransform(correction.Transform);

                result.Add(id, correction);
            }

            return result;
        }


        private void ValidateCorrectionTarget(FullBodyJointMETA id)
        {
            var index = (int)id;

            if (index < 0 || index >= _mapped.Length || _mapped[index] == null)
                throw new ArgumentException("Correction target must be a mapped joint.");
        }


        private void ValidateCorrectionReference(FullBodyJointMETA id)
        {
            var index = (int)id;

            if (index < 0 || index >= _mapped.Length)
                throw new ArgumentException("Correction reference joint is invalid.");
        }


        private static void ValidateCorrectionTransform(Matrix4x4 transform)
        {
            if (!IsFinite(transform))
                throw new ArgumentException("Skin correction contains invalid values.");

            if (!IsAffine(transform))
                throw new ArgumentException("Skin correction must be affine.");

            if (!Matrix4x4.Invert(transform, out _))
                throw new ArgumentException("Skin correction must be invertible.");
        }


        private Matrix4x4[][] CreateSkinBinds(
            Dictionary<Joint3D, Matrix4x4> fittedBind,
            Dictionary<Joint3D, Matrix4x4> jointBind,
            Matrix4x4[] trackingRest,
            Dictionary<FullBodyJointMETA, SkinBindingCorrection> corrections)
        {
            var result = _skins.Select(s => (Matrix4x4[])s.Bind.Clone()).ToArray();

            for (var skinIndex = 0; skinIndex < _skins.Length; skinIndex++)
            {
                var skin = _skins[skinIndex].Skin;

                for (var slot = 0; slot < skin.Joints!.Length; slot++)
                {
                    var joint = skin.Joints[slot];

                    if (!jointBind.TryGetValue(joint, out var bindWorld))
                        continue;

                    var fitted = fittedBind[joint];

                    if (_indices.TryGetValue(joint, out var jointIndex) &&
                        corrections.TryGetValue((FullBodyJointMETA)jointIndex, out var correction) &&
                        correction.Transform != Matrix4x4.Identity)
                    {
                        var reference = trackingRest[(int)correction.ReferenceJoint];

                        fitted = fitted
                            * Inverse(reference)
                            * correction.Transform
                            * reference;
                    }

                    result[skinIndex][slot] = result[skinIndex][slot]
                        * fitted
                        * Inverse(bindWorld);
                }
            }

            return result;
        }


        private Dictionary<Joint3D, Matrix4x4> Fit(Matrix4x4[] trackingRest)
        {
            var facing = CreateFacingTransform(trackingRest);
            var fitted = new Dictionary<Joint3D, Matrix4x4>();
            var rotations = new Dictionary<Joint3D, Matrix4x4>();

            foreach (var joint in _originalOrder)
            {
                var parent = _originalParents[joint];

                if (!_indices.TryGetValue(joint, out var index))
                {
                    fitted[joint] = _originalLocal[joint] * fitted[parent!];
                    rotations[joint] = rotations[parent!];
                    continue;
                }

                var id = (FullBodyJointMETA)index;

                if (TryFitFootBall(id, joint, fitted, rotations))
                    continue;

                FitMappedJoint(joint, index, id, parent, trackingRest, facing, fitted, rotations);
            }

            FitBodyRegions(trackingRest, facing, fitted);
            RefitHelperJoints(fitted);

            return fitted;
        }


        private Matrix4x4 CreateFacingTransform(Matrix4x4[] trackingRest)
        {
            var leftIndex = (int)FullBodyJointMETA.LeftUpperLegMeta;
            var rightIndex = (int)FullBodyJointMETA.RightUpperLegMeta;

            if (leftIndex >= _mapped.Length || rightIndex >= _mapped.Length)
                return Matrix4x4.Identity;

            var left = _mapped[leftIndex];
            var right = _mapped[rightIndex];

            if (left == null || right == null)
                return Matrix4x4.Identity;

            var from = _originalWorld[right].Translation - _originalWorld[left].Translation;
            var to = trackingRest[rightIndex].Translation - trackingRest[leftIndex].Translation;

            if (from.X * from.X + from.Z * from.Z <= 1e-10f)
                return Matrix4x4.Identity;

            if (to.X * to.X + to.Z * to.Z <= 1e-10f)
                return Matrix4x4.Identity;

            var angle = MathF.Atan2(from.Z, from.X) - MathF.Atan2(to.Z, to.X);

            return Matrix4x4.CreateRotationY(angle);
        }


        private bool TryFitFootBall(FullBodyJointMETA id, Joint3D joint,
            Dictionary<Joint3D, Matrix4x4> fitted,
            Dictionary<Joint3D, Matrix4x4> rotations)
        {
            FullBodyJointMETA ankleId;

            if (id == FullBodyJointMETA.LeftFootBallMeta)
                ankleId = FullBodyJointMETA.LeftFootAnkleMeta;
            else if (id == FullBodyJointMETA.RightFootBallMeta)
                ankleId = FullBodyJointMETA.RightFootAnkleMeta;
            else
                return false;

            var ankle = _mapped[(int)ankleId];

            if (ankle == null)
                return false;

            fitted[joint] = _originalWorld[joint]
                * Inverse(_originalWorld[ankle])
                * fitted[ankle];

            rotations[joint] = rotations[ankle];

            return true;
        }


        private void FitMappedJoint(Joint3D joint, int index, FullBodyJointMETA id, Joint3D? parent,
            Matrix4x4[] trackingRest, Matrix4x4 facing,
            Dictionary<Joint3D, Matrix4x4> fitted,
            Dictionary<Joint3D, Matrix4x4> rotations)
        {
            var original = _originalWorld[joint];
            var linear = facing;
            var rotation = facing;

            var childIndex = FindDirectionChild(joint, index);

            if (id != FullBodyJointMETA.HeadMeta && index != _rootIndex && childIndex >= 0)
            {
                var child = _mapped[childIndex]!;

                var from = Vector3.TransformNormal(
                    _originalWorld[child].Translation - original.Translation,
                    facing);

                var to = trackingRest[childIndex].Translation - trackingRest[index].Translation;

                if (from.LengthSquared() > 1e-10f && to.LengthSquared() > 1e-10f)
                {
                    var direction = Vector3.Normalize(to);

                    linear *= Matrix4x4.CreateFromQuaternion(
                        FromTo(Vector3.Normalize(from), direction));

                    rotation = linear;

                    if (!IsFoot(id))
                    {
                        var scale = to.Length() / from.Length();
                        linear *= CreateDirectionalScale(direction, scale);
                    }
                }
            }
            else if (childIndex < 0 && parent != null)
            {
                linear = rotations[parent];
                rotation = rotations[parent];
            }

            rotations[joint] = rotation;

            fitted[joint] = original
                * Matrix4x4.CreateTranslation(-original.Translation)
                * linear
                * Matrix4x4.CreateTranslation(trackingRest[index].Translation);
        }


        private int FindDirectionChild(Joint3D joint, int index)
        {
            var middle = index == (int)FullBodyJointMETA.LeftHandWristMeta
                ? (int)FullBodyJointMETA.LeftHandMiddleProximalMeta
                : index == (int)FullBodyJointMETA.RightHandWristMeta
                    ? (int)FullBodyJointMETA.RightHandMiddleProximalMeta
                    : -1;

            if (middle >= 0 && middle < _mapped.Length && _mapped[middle] != null)
                return middle;

            var bestIndex = -1;
            var bestDistance = int.MaxValue;

            foreach (var pair in _indices)
            {
                if (pair.Key == joint)
                    continue;

                var distance = DistanceTo(pair.Key, joint);

                if (distance > bestDistance)
                    continue;

                if (distance == bestDistance && pair.Value >= bestIndex)
                    continue;

                bestDistance = distance;
                bestIndex = pair.Value;
            }

            return bestIndex;
        }


        private void FitBodyRegions(Matrix4x4[] trackingRest, Matrix4x4 facing,
            Dictionary<Joint3D, Matrix4x4> fitted)
        {
            FitTorso(trackingRest, facing, fitted);
            FitNeck(fitted);
        }


        private void FitTorso(Matrix4x4[] trackingRest, Matrix4x4 facing,
            Dictionary<Joint3D, Matrix4x4> fitted)
        {
            var hipsIndex = (int)FullBodyJointMETA.HipsMeta;
            var chestIndex = (int)FullBodyJointMETA.ChestMeta;

            if (hipsIndex >= _mapped.Length || chestIndex >= _mapped.Length)
                return;

            var hips = _mapped[hipsIndex];
            var chest = _mapped[chestIndex];

            if (hips == null || chest == null)
                return;

            var origin = _originalWorld[hips].Translation;
            var from = Vector3.TransformNormal(_originalWorld[chest].Translation - origin, facing);
            var to = trackingRest[chestIndex].Translation - trackingRest[hipsIndex].Translation;

            if (from.LengthSquared() <= 1e-10f || to.LengthSquared() <= 1e-10f)
                return;

            var direction = Vector3.Normalize(to);
            var rotation = Matrix4x4.CreateFromQuaternion(
                FromTo(Vector3.Normalize(from), direction));

            var scale = to.Length() / from.Length();

            var deformation = Matrix4x4.CreateTranslation(-origin)
                * facing
                * rotation
                * CreateDirectionalScale(direction, scale)
                * Matrix4x4.CreateTranslation(trackingRest[hipsIndex].Translation);

            foreach (var pair in _indices)
            {
                if (IsTorso((FullBodyJointMETA)pair.Value))
                    fitted[pair.Key] = _originalWorld[pair.Key] * deformation;
            }
        }


        private void FitNeck(Dictionary<Joint3D, Matrix4x4> fitted)
        {
            var headIndex = (int)FullBodyJointMETA.HeadMeta;
            var neckIndex = (int)FullBodyJointMETA.NeckMeta;

            if (headIndex >= _mapped.Length || neckIndex >= _mapped.Length)
                return;

            var head = _mapped[headIndex];
            var neck = _mapped[neckIndex];

            if (head == null || neck == null)
                return;

            var headDeformation = Inverse(_originalWorld[head]) * fitted[head];

            fitted[neck] = _originalWorld[neck] * headDeformation;
        }


        private void RefitHelperJoints(Dictionary<Joint3D, Matrix4x4> fitted)
        {
            foreach (var joint in _originalOrder)
            {
                if (_indices.ContainsKey(joint))
                    continue;

                fitted[joint] = _originalLocal[joint] * fitted[_originalParents[joint]!];
            }
        }


        private int DistanceTo(Joint3D child, Joint3D ancestor)
        {
            var depth = 0;

            for (Joint3D? current = child; current != null; current = _originalParents[current])
            {
                if (current == ancestor)
                    return depth;

                depth++;
            }

            return int.MaxValue;
        }


        private static Matrix4x4 CreateDirectionalScale(Vector3 direction, float scale)
        {
            var k = scale - 1;
            var x = direction.X;
            var y = direction.Y;
            var z = direction.Z;

            return new Matrix4x4(
                1 + k * x * x, k * x * y, k * x * z, 0,
                k * y * x, 1 + k * y * y, k * y * z, 0,
                k * z * x, k * z * y, 1 + k * z * z, 0,
                0, 0, 0, 1);
        }


        private static Quaternion FromTo(Vector3 from, Vector3 to)
        {
            var dot = Math.Clamp(Vector3.Dot(from, to), -1, 1);

            if (dot < -0.999999f)
            {
                var axis = Vector3.Cross(from,
                    MathF.Abs(from.X) < .9f ? Vector3.UnitX : Vector3.UnitY);

                return Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), MathF.PI);
            }

            return Quaternion.Normalize(new Quaternion(Vector3.Cross(from, to), 1 + dot));
        }


        private static bool IsTorso(FullBodyJointMETA id) => id is
            FullBodyJointMETA.HipsMeta or FullBodyJointMETA.SpineLowerMeta or
            FullBodyJointMETA.SpineMiddleMeta or FullBodyJointMETA.SpineUpperMeta or
            FullBodyJointMETA.ChestMeta;


        private static bool IsFoot(FullBodyJointMETA id) => id is
            FullBodyJointMETA.LeftFootAnkleMeta or FullBodyJointMETA.RightFootAnkleMeta or
            FullBodyJointMETA.LeftFootBallMeta or FullBodyJointMETA.RightFootBallMeta;


        private static bool IsFinite(Matrix4x4 m)
        {
            return Valid(new Vector3(m.M11, m.M12, m.M13)) &&
                   Valid(new Vector3(m.M21, m.M22, m.M23)) &&
                   Valid(new Vector3(m.M31, m.M32, m.M33)) &&
                   Valid(m.Translation);
        }


        private static bool IsAffine(Matrix4x4 m)
        {
            return m.M14 == 0 &&
                   m.M24 == 0 &&
                   m.M34 == 0 &&
                   m.M44 == 1;
        }


        private static Matrix4x4 Pose(Vector3 position, Quaternion orientation)
            => Matrix4x4.CreateFromQuaternion(orientation) * Matrix4x4.CreateTranslation(position);


        private static Matrix4x4 Inverse(Matrix4x4 value)
        {
            if (!Matrix4x4.Invert(value, out var inverse))
                throw new ArgumentException("Singular bind transform.");

            return inverse;
        }


        private static bool Valid(Vector3 value)
            => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);


        private static bool Valid(Quaternion value)
            => float.IsFinite(value.LengthSquared()) && value.LengthSquared() > 1e-12f;
    }
}