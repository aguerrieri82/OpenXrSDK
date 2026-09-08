using OpenXr.Framework;
using Silk.NET.OpenXR;
using System.Numerics;

namespace XrEngine.OpenXr.Oculus
{
    /// <summary>
    /// Rebuilds the weighted avatar joints on the full XR hierarchy. Geometry fitting
    /// is absorbed into inverse binds; the joint frames themselves remain exactly XR.
    /// Uses row matrices and reference-space tracking poses, as does MeshSkin.
    /// </summary>
    public sealed class BodySkeletonRetargeter
    {
        /// <summary>Skin-only affine adjustment in ReferenceJoint's XR bind-local axes.
        /// Translations use tracking units (meters). Does not propagate to other joints.</summary>
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

            _originalOrder = new[] { root }.Concat(root.Descendants().OfType<Joint3D>()).ToArray();
            _originalWorld = _originalOrder.ToDictionary(j => j, j => j.WorldMatrix);
            _originalLocal = _originalOrder.ToDictionary(j => j, j => j.Transform.Matrix);
            _originalParents = _originalOrder.ToDictionary(j => j, j => j.Parent as Joint3D);
            
            if (_originalOrder.Any(j => j != root && (_originalParents[j] == null || !_originalWorld.ContainsKey(_originalParents[j]!))))
                throw new ArgumentException("Avatar bones must form a joint hierarchy without intermediate non-joint groups.");
           
            _indices = new Dictionary<Joint3D, int>();
            
            for (var i = 0; i < _mapped.Length; i++)
            {
                if (_mapped[i] is not { } joint)
                    continue;

                if (!_originalWorld.ContainsKey(joint) || !_indices.TryAdd(joint, i))
                    throw new ArgumentException("Mapped joints must be distinct descendants of the root.");
            }

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

        /// <summary>Replaces all corrections; null/empty restores the automatic fit.
        /// Can be called while tracking without resetting the current joint poses.</summary>
        public void SetSkinCorrections(IReadOnlyDictionary<FullBodyJointMETA, SkinBindingCorrection>? corrections)
        {
            var copy = ValidateCorrections(corrections);

            if (_fittedBind != null && _jointBind != null)
            {
                var binds = CreateSkinBinds(_fittedBind, _jointBind, _trackingBind, copy);

                for (var k = 0; k < _skins.Length; k++)
                    _skins[k].Skin.InverseBindMatrices = binds[k];
            }
            
            _skinCorrections = copy;
        }   

        private Dictionary<FullBodyJointMETA, SkinBindingCorrection> ValidateCorrections(
            IReadOnlyDictionary<FullBodyJointMETA, SkinBindingCorrection>? corrections)
        {
            var copy = new Dictionary<FullBodyJointMETA, SkinBindingCorrection>();
            
            if (corrections == null) 
                return copy;

            foreach (var (id, correction) in corrections)
            {
                var i = (int)id;
                var frame = (int)correction.ReferenceJoint;
                var m = correction.Transform;
                
                if (i < 0 || i >= _mapped.Length || _mapped[i] == null || frame < 0 || frame >= _mapped.Length)
                    throw new ArgumentException("Correction requires a mapped target and a valid XR reference joint.");
                
                if (!Valid(new Vector3(m.M11, m.M12, m.M13)) || !Valid(new Vector3(m.M21, m.M22, m.M23)) ||
                    !Valid(new Vector3(m.M31, m.M32, m.M33)) || !Valid(m.Translation) ||
                    m.M14 != 0 || m.M24 != 0 || m.M34 != 0 || m.M44 != 1 ||
                    !float.IsFinite(m.GetDeterminant()) || !Matrix4x4.Invert(m, out _))
                    throw new ArgumentException("Skin correction must be a finite, invertible affine matrix.");

                copy.Add(id, correction);
            }
            return copy;
        }

        private Matrix4x4[][] CreateSkinBinds(Dictionary<Joint3D, Matrix4x4> fitted,
            Dictionary<Joint3D, Matrix4x4> jointRest, Matrix4x4[] rest,
            Dictionary<FullBodyJointMETA, SkinBindingCorrection> corrections)
        {
            var binds = _skins.Select(s => (Matrix4x4[])s.Bind.Clone()).ToArray();

            for (var k = 0; k < _skins.Length; k++)
            {
                for (var slot = 0; slot < _skins[k].Skin.Joints!.Length; slot++)
                {
                    var joint = _skins[k].Skin.Joints![slot];

                    if (!jointRest.TryGetValue(joint, out var bindWorld))
                        continue;

                    var fit = fitted[joint];

                    if (_indices.TryGetValue(joint, out var i) && 
                        corrections.TryGetValue((FullBodyJointMETA)i, out var correction)
                        && correction.Transform != Matrix4x4.Identity)
                    {
                        var frame = rest[(int)correction.ReferenceJoint];
                        fit = fit * Inverse(frame) * correction.Transform * frame;
                    }

                    binds[k][slot] = binds[k][slot] * fit * Inverse(bindWorld);
                }
            }
               
            return binds;
        }

        public bool IsBoundTo(BodySkeletonJointFB[] skeleton) 
            => ReferenceEquals(_sourceRest, skeleton);

        public void Rebind(BodySkeletonJointFB[] sourceRest)
        {
            if (sourceRest.Length != _mapped.Length)
                throw new ArgumentException("XR skeleton and mapping lengths differ.", nameof(sourceRest));

            // Validate before changing scene objects.
            var order = new List<int>();
            var visited = new byte[sourceRest.Length];

            void Visit(int i)
            {
                if (visited[i] == 2) 
                    return;

                if (visited[i] == 1) 
                    throw new ArgumentException("XR skeleton contains a cycle.");

                visited[i] = 1;

                var parent = sourceRest[i].ParentJoint;

                if (parent < -1 || parent >= sourceRest.Length || (parent == -1) != (i == _rootIndex))
                    throw new ArgumentException("Invalid XR parent joint.");

                if (parent >= 0) 
                    Visit(parent);

                visited[i] = 2;

                order.Add(i);
            }

            var rest = new Matrix4x4[sourceRest.Length];

            for (var i = 0; i < sourceRest.Length; i++)
            {
                Visit(i);
                
                var pose = sourceRest[i].Pose.ToPose3();
                
                if (!Valid(pose.Position) || !Valid(pose.Orientation))
                    throw new ArgumentException("XR bind pose is invalid.");

                rest[i] = Pose(pose.Position, Quaternion.Normalize(pose.Orientation));
            }

            var fitted = Fit(rest);
            var jointRest = new Dictionary<Joint3D, Matrix4x4>();

            foreach (var joint in _originalOrder)
            {
                if (_indices.TryGetValue(joint, out var i))
                    jointRest[joint] = rest[i];
                else
                {
                    // Keep affine stretch/shear in the skin bind, never in a Joint3D
                    // transform (the scene graph stores only translation/rotation/scale).
                    if (!Matrix4x4.Decompose(fitted[joint], out _, out var rotation, out var position))
                    {
                        // A sheared fit can fail Decompose. Use the original frame;
                        // the full fitted matrix still goes into the inverse bind.
                        rotation = Quaternion.CreateFromRotationMatrix(_originalWorld[joint]);
                        position = fitted[joint].Translation;
                    }

                    jointRest[joint] = Pose(position, Quaternion.Normalize(rotation));
                }
            }

            // Always start from original binds, including on calibration revisions.
            var binds = CreateSkinBinds(fitted, jointRest, rest, _skinCorrections);

            // Detach first, avoiding transient cycles on topology changes.
            for (var i = 0; i < _joints.Length; i++)
            {
                _joints[i] ??= _mapped[i] ?? new Joint3D { Name = "XR_" + (FullBodyJointMETA)i };
                _joints[i].Parent?.RemoveChild(_joints[i]);
            }

            foreach (var i in order)
            {
                var parent = sourceRest[i].ParentJoint;
                (parent < 0 ? _container : _joints[parent]).AddChild(_joints[i]);
                _joints[i].Transform.LocalPivot = Vector3.Zero;
                _joints[i].WorldMatrix = rest[i];
                _positions[i] = rest[i].Translation;
                _orientations[i] = Quaternion.CreateFromRotationMatrix(rest[i]);
            }

            // Preserve the fitted frames of avatar-only weighted helpers rather than
            // blindly letting them inherit their parent's newly assigned XR axes.
            foreach (var joint in _originalOrder)
            {
                if (!_indices.ContainsKey(joint))
                    joint.WorldMatrix = jointRest[joint];
            }

            for (var k = 0; k < _skins.Length; k++)
                _skins[k].Skin.InverseBindMatrices = binds[k];

            _order = order.ToArray();
            _sourceRest = sourceRest;
            _fittedBind = fitted;
            _jointBind = jointRest;
            _trackingBind = rest;
        }

        private Dictionary<Joint3D, Matrix4x4> Fit(Matrix4x4[] rest)
        {
            // Infer facing from the hip line; no post-update root rotation.
            var facing = Matrix4x4.Identity;
            var left = (int)FullBodyJointMETA.LeftUpperLegMeta;
            var right = (int)FullBodyJointMETA.RightUpperLegMeta;

            if (left < _mapped.Length && right < _mapped.Length && _mapped[left] is { } l && _mapped[right] is { } r)
            {
                var from = _originalWorld[r].Translation - _originalWorld[l].Translation;
                var to = rest[right].Translation - rest[left].Translation;

                if (from.X * from.X + from.Z * from.Z > 1e-10f && to.X * to.X + to.Z * to.Z > 1e-10f)
                    facing = Matrix4x4.CreateRotationY(MathF.Atan2(from.Z, from.X) - MathF.Atan2(to.Z, to.X));
            }

            var result = new Dictionary<Joint3D, Matrix4x4>();
            var rotations = new Dictionary<Joint3D, Matrix4x4>();

            foreach (var joint in _originalOrder)
            {
                var parent = _originalParents[joint];
                if (!_indices.TryGetValue(joint, out var i))
                {
                    result[joint] = _originalLocal[joint] * result[parent!];
                    rotations[joint] = rotations[parent!];
                    continue;
                }

                var old = _originalWorld[joint];
                var linear = facing;
                var rotation = facing;
                var id = (FullBodyJointMETA)i;
                
                var ankleId = id == FullBodyJointMETA.LeftFootBallMeta ? 
                      FullBodyJointMETA.LeftFootAnkleMeta
                    : FullBodyJointMETA.RightFootAnkleMeta;

                if ((id == FullBodyJointMETA.LeftFootBallMeta || id == FullBodyJointMETA.RightFootBallMeta)
                    && _mapped[(int)ankleId] is { } ankle)
                {
                    // One rigid calibration for the whole foot. The XR ball joint still
                    // matches tracking, but its skin bind absorbs the toe-length offset.
                    // Otherwise blending ankle and ball weights would stretch the foot.
                    result[joint] = old * Inverse(_originalWorld[ankle]) * result[ankle];
                    rotations[joint] = rotations[ankle];
                    continue;
                }

                // Closest mapped descendant determines the longitudinal direction.
                // Wrists use the middle finger rather than a thumb/index branch.
                var child = _indices.Where(p => p.Key != joint && DistanceTo(p.Key, joint) < int.MaxValue)
                    .OrderBy(p => DistanceTo(p.Key, joint)).ThenBy(p => p.Value)
                    .Select(p => (int?)p.Value).FirstOrDefault();
                
                var middle = i == (int)FullBodyJointMETA.LeftHandWristMeta
                    ? (int)FullBodyJointMETA.LeftHandMiddleProximalMeta
                    : i == (int)FullBodyJointMETA.RightHandWristMeta
                        ? (int)FullBodyJointMETA.RightHandMiddleProximalMeta : -1;


                if (middle >= 0 && middle < _mapped.Length && _mapped[middle] != null) 
                    child = middle;

                // A neck-length correction must never resize the skull or face. Root
                // displacement likewise says nothing about the size of its geometry.
                if (id == FullBodyJointMETA.HeadMeta || i == _rootIndex)
                {
                    linear = facing;
                }
                else if (child is { } c)
                {
                    var from = Vector3.TransformNormal(_originalWorld[_mapped[c]!].Translation - old.Translation, facing);
                    var to = rest[c].Translation - rest[i].Translation;

                    if (from.LengthSquared() > 1e-10f && to.LengthSquared() > 1e-10f)
                    {
                        var direction = Vector3.Normalize(to);
                        
                        linear *= Matrix4x4.CreateFromQuaternion(FromTo(Vector3.Normalize(from), direction));
                        rotation = linear;
                        
                        // Hands and limbs follow measured segment dimensions.
                        // Regional torso/neck fitting is applied below.
                        
                        var scale = IsFoot(id) ? 1 : to.Length() / from.Length();
                        var k = scale - 1;
                        var x = direction.X; var y = direction.Y; var z = direction.Z;

                        linear *= new Matrix4x4(1+k*x*x, k*x*y, k*x*z, 0,
                            k*y*x, 1+k*y*y, k*y*z, 0, k*z*x, k*z*y, 1+k*z*z, 0, 0, 0, 0, 1);
                    }
                }
                else if (parent != null)
                {
                    // End bones inherit swing, not their parent's length correction.
                    // In particular the foot ball must not inherit lower-leg stretch.
                    linear = rotation = rotations[parent];
                }

                rotations[joint] = rotation;
                result[joint] = old * Matrix4x4.CreateTranslation(-old.Translation)
                    * linear * Matrix4x4.CreateTranslation(rest[i].Translation);
            }
            
            FitBodyRegions(rest, facing, result);
            
            // Recompute avatar-only helpers after replacing the regional calibrations.
            
            foreach (var joint in _originalOrder)
            {
                if (!_indices.ContainsKey(joint))
                    result[joint] = _originalLocal[joint] * result[_originalParents[joint]!];
            }

            return result;
        }

        private void FitBodyRegions(Matrix4x4[] rest, Matrix4x4 facing, Dictionary<Joint3D, Matrix4x4> fitted)
        {
            var hipsIndex = (int)FullBodyJointMETA.HipsMeta;
            var chestIndex = (int)FullBodyJointMETA.ChestMeta;
            var headIndex = (int)FullBodyJointMETA.HeadMeta;
            var neckIndex = (int)FullBodyJointMETA.NeckMeta;

            if (chestIndex < _mapped.Length && _mapped[hipsIndex] is { } hips && _mapped[chestIndex] is { } chest)
            {
                var origin = _originalWorld[hips].Translation;
                var from = Vector3.TransformNormal(_originalWorld[chest].Translation - origin, facing);
                var to = rest[chestIndex].Translation - rest[hipsIndex].Translation;

                if (from.LengthSquared() > 1e-10f && to.LengthSquared() > 1e-10f)
                {
                    var d = Vector3.Normalize(to);
                    var k = to.Length() / from.Length() - 1;
                    
                    var stretch = new Matrix4x4(1+k*d.X*d.X, k*d.X*d.Y, k*d.X*d.Z, 0,
                        k*d.Y*d.X, 1+k*d.Y*d.Y, k*d.Y*d.Z, 0,
                        k*d.Z*d.X, k*d.Z*d.Y, 1+k*d.Z*d.Z, 0, 0, 0, 0, 1);

                    var deformation = Matrix4x4.CreateTranslation(-origin) * facing
                        * Matrix4x4.CreateFromQuaternion(FromTo(Vector3.Normalize(from), d))
                        * stretch * Matrix4x4.CreateTranslation(rest[hipsIndex].Translation);

                    // One affine map for the entire torso, including its translations.
                    // Sharing only a scale still gave every spine influence a different
                    // pivot and rotation, producing a kink even in the reference pose.
                    foreach (var pair in _indices)
                    {
                        if (IsTorso((FullBodyJointMETA)pair.Value))
                            fitted[pair.Key] = _originalWorld[pair.Key] * deformation;
                    }
                }
            }

            if (headIndex < _mapped.Length && _mapped[headIndex] is { } head && _mapped[neckIndex] is { } neck)
            {
                // Preserve the original neck-to-head distance in the SKIN, anchored at
                // the tracked head. The XR neck joint remains exact; its inverse bind
                // absorbs this anatomical offset instead of compressing the neck.
                var headDeformation = Inverse(_originalWorld[head]) * fitted[head];
                fitted[neck] = _originalWorld[neck] * headDeformation;
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

        private static Quaternion FromTo(Vector3 from, Vector3 to)
        {
            var dot = Math.Clamp(Vector3.Dot(from, to), -1, 1);
            
            if (dot < -0.999999f)
            {
                var axis = Vector3.Cross(from, MathF.Abs(from.X) < .9f ? Vector3.UnitX : Vector3.UnitY);

                return Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), MathF.PI);
            }

            return Quaternion.Normalize(new Quaternion(Vector3.Cross(from, to), 1 + dot));
        }

        public void Update(BodyJointLocationFB[] locations, Vector3 rootDelta)
        {
            if (locations.Length != _joints.Length || !Valid(rootDelta))
                throw new ArgumentException("Invalid tracking frame or root offset.");

            foreach (var i in _order)
            {
                var location = locations[i];
                var pose = location.Pose.ToPose3();

                if ((location.LocationFlags & SpaceLocationFlags.PositionValidBit) != 0 && Valid(pose.Position))
                    _positions[i] = pose.Position;
                
                if ((location.LocationFlags & SpaceLocationFlags.OrientationValidBit) != 0 && Valid(pose.Orientation))
                    _orientations[i] = Quaternion.Normalize(pose.Orientation);

                // Hold invalid components in reference space, even when parents move.
                // WorldMatrix also removes old scales and bypasses position deadbands.
                _joints[i].WorldMatrix = Pose(_positions[i] + rootDelta, _orientations[i]);
            }
        }

        private static bool IsTorso(FullBodyJointMETA id) => id is
            FullBodyJointMETA.HipsMeta or FullBodyJointMETA.SpineLowerMeta or
            FullBodyJointMETA.SpineMiddleMeta or FullBodyJointMETA.SpineUpperMeta or FullBodyJointMETA.ChestMeta;

        private static bool IsFoot(FullBodyJointMETA id) => id is
            FullBodyJointMETA.LeftFootAnkleMeta or FullBodyJointMETA.RightFootAnkleMeta or
            FullBodyJointMETA.LeftFootBallMeta or FullBodyJointMETA.RightFootBallMeta;

        private static Matrix4x4 Pose(Vector3 p, Quaternion q) =>
            Matrix4x4.CreateFromQuaternion(q) * Matrix4x4.CreateTranslation(p);

        private static Matrix4x4 Inverse(Matrix4x4 value) => Matrix4x4.Invert(value, out var inverse)
            ? inverse : throw new ArgumentException("Singular bind transform.");

        private static bool Valid(Vector3 p) => 
            float.IsFinite(p.X) && float.IsFinite(p.Y) && float.IsFinite(p.Z);

        private static bool Valid(Quaternion q) => 
            float.IsFinite(q.LengthSquared()) && q.LengthSquared() > 1e-12f;
    }
}
