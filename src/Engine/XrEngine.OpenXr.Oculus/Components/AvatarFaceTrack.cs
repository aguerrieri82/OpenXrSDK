using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using System.Numerics;
using XrEngine.Components;
using XrMath;


namespace XrEngine.OpenXr.Oculus
{
    public class AvatarFaceTrack : Behavior<Avatar>
    {
        public struct JointBlend
        {
            public Pose3 Start;
            public Pose3 End;
        }

        XrFaceTrack? _faceTrack;
        XrApp? _xrApp;

        MeshMorph? _morph;
        Dictionary<string, int>? _targets;
        readonly Dictionary<string, float> _morphWeights = new();
        readonly List<CorrectiveMorph> _correctiveMorphs = new();

        private class CorrectiveMorph
        {
            public string Name = "";
            public string[] Drivers = Array.Empty<string>();
        }

        Joint3D? _eyeLeft;
        Joint3D? _eyeRight;
        Joint3D? _dentureBottom;
        Joint3D? _tongueBase;
        Joint3D? _tongueTip;

        JointBlend _eyeLeftUp;
        JointBlend _eyeLeftDown;
        JointBlend _eyeLeftLeft;
        JointBlend _eyeLeftRight;

        JointBlend _eyeRightUp;
        JointBlend _eyeRightDown;
        JointBlend _eyeRightLeft;
        JointBlend _eyeRightRight;

        JointBlend _jawDrop;
        JointBlend _jawLeft;
        JointBlend _jawRight;
        JointBlend _jawThrust;

        JointBlend _tongueOut;
        JointBlend _tongueRetreat;
        JointBlend _tongueTipInterdental;
        JointBlend _tongueTipAlveolar;
        JointBlend _tongueFrontDorsal;
        JointBlend _tongueMidDorsal;
        JointBlend _tongueBackDorsal;

        public bool UseApproximateMorphs { get; set; }

        protected override void Start(RenderContext ctx)
        {
            var mesh = _host!.FindByName<TriangleMesh>("LOD00_combined_geometry,0")
                ?? throw new InvalidOperationException("Avatar face mesh not found");

            _morph = mesh.Component<MeshMorph>()!;

            var geometry = mesh.Geometry!.Component<MorphedGeometry>()!;

            _targets = geometry.Targets!
                .Select((target, index) => (target.Name, index))
                .ToDictionary(a => a.Name!, a => a.index);

            _eyeLeft = _host.FindByName<Joint3D>("eye_left_joint");
            _eyeRight = _host.FindByName<Joint3D>("eye_right_joint");
            _dentureBottom = _host.FindByName<Joint3D>("dentureBt_joint");
            _tongueBase = _host.FindByName<Joint3D>("tongueBase_joint");
            _tongueTip = _host.FindByName<Joint3D>("tongueTip_joint");

            BuildCorrectiveMappings();
            BuildSkinMappings();
        }

        private void BuildCorrectiveMappings()
        {
            _correctiveMorphs.Clear();

            foreach (var name in _targets!.Keys)
            {
                if (!name.StartsWith("anim_", StringComparison.Ordinal))
                    continue;

                var parts = name.Substring(5).Split('_');
                var suffix = parts[parts.Length - 1];
                var hasSuffix = suffix == "L" || suffix == "R" ||
                    suffix == "LT" || suffix == "RT" || suffix == "LB" || suffix == "RB" ||
                    suffix == "T" || suffix == "B";
                var driverCount = hasSuffix ? parts.Length - 1 : parts.Length;

                if (driverCount < 2)
                    continue;

                var drivers = new string[driverCount];
                var complete = true;
                for (var i = 0; i < driverCount; i++)
                {
                    var driver = "anim_" + parts[i];
                    if (hasSuffix && _targets.ContainsKey(driver + "_" + suffix))
                        driver += "_" + suffix;
                    else if (hasSuffix && suffix.Length == 2 && _targets.ContainsKey(driver + "_" + suffix[0]))
                        driver += "_" + suffix[0];
                    else if (hasSuffix && suffix.Length == 2 && _targets.ContainsKey(driver + "_" + suffix[1]))
                        driver += "_" + suffix[1];
                    else if (!_targets.ContainsKey(driver))
                    {
                        complete = false;
                        break;
                    }

                    drivers[i] = driver;
                }

                if (complete)
                    _correctiveMorphs.Add(new CorrectiveMorph { Name = name, Drivers = drivers });
            }
        }

        private void UpdateCorrectiveMorphs()
        {
            // Legacy SDK combined shapes follow the product rule used by Unity Movement.
            // Only use drivers written this frame; unsupported controls must not activate a corrective.
            foreach (var corrective in _correctiveMorphs)
            {
                var value = 1f;
                foreach (var driver in corrective.Drivers)
                {
                    if (!_morphWeights.TryGetValue(driver, out var weight))
                    {
                        value = 0;
                        break;
                    }

                    value *= weight;
                }

                Set(corrective.Name, value);
            }
        }

        private void BuildSkinMappings()
        {
            const float eyePitch = 25f * MathF.PI / 180f;
            const float eyeYaw = 30f * MathF.PI / 180f;

            const float jawDrop = 0.012f;
            const float jawDropBack = 0.004f;
            const float jawSideways = 0.008f;
            const float jawThrust = 0.008f;

            const float tongueOut = 0.030f;
            const float tongueRetreat = 0.015f;

            const float tongueInterdental = 10f * MathF.PI / 180f;
            const float tongueAlveolar = 25f * MathF.PI / 180f;
            const float tongueFrontDorsal = 20f * MathF.PI / 180f;
            const float tongueMidDorsal = 12f * MathF.PI / 180f;
            const float tongueBackDorsal = 8f * MathF.PI / 180f;

            if (_eyeLeft != null)
            {
                var start = GetPose(_eyeLeft);

                _eyeLeftUp = CreateBlend(start, Vector3.Zero, new Vector3(-eyePitch, 0, 0));
                _eyeLeftDown = CreateBlend(start, Vector3.Zero, new Vector3(eyePitch, 0, 0));
                _eyeLeftLeft = CreateBlend(start, Vector3.Zero, new Vector3(0, eyeYaw, 0));
                _eyeLeftRight = CreateBlend(start, Vector3.Zero, new Vector3(0, -eyeYaw, 0));
            }

            if (_eyeRight != null)
            {
                var start = GetPose(_eyeRight);

                _eyeRightUp = CreateBlend(start, Vector3.Zero, new Vector3(-eyePitch, 0, 0));
                _eyeRightDown = CreateBlend(start, Vector3.Zero, new Vector3(eyePitch, 0, 0));
                _eyeRightLeft = CreateBlend(start, Vector3.Zero, new Vector3(0, eyeYaw, 0));
                _eyeRightRight = CreateBlend(start, Vector3.Zero, new Vector3(0, -eyeYaw, 0));
            }

            if (_dentureBottom != null)
            {
                var start = GetPose(_dentureBottom);

                _jawDrop = CreateBlend(start, new Vector3(jawDropBack, 0, -jawDrop), Vector3.Zero);
                _jawLeft = CreateBlend(start, new Vector3(0, -jawSideways, 0), Vector3.Zero);
                _jawRight = CreateBlend(start, new Vector3(0, jawSideways, 0), Vector3.Zero);
                _jawThrust = CreateBlend(start, new Vector3(-jawThrust, 0, 0), Vector3.Zero);
            }

            if (_tongueBase != null)
            {
                var start = GetPose(_tongueBase);

                _tongueOut = CreateBlend(start, new Vector3(-tongueOut, 0, 0), Vector3.Zero);
                _tongueRetreat = CreateBlend(start, new Vector3(tongueRetreat, 0, 0), Vector3.Zero);

                _tongueFrontDorsal = CreateBlend(
                    start,
                    Vector3.Zero,
                    new Vector3(0, -tongueFrontDorsal, 0));

                _tongueMidDorsal = CreateBlend(
                    start,
                    Vector3.Zero,
                    new Vector3(0, -tongueMidDorsal, 0));

                _tongueBackDorsal = CreateBlend(
                    start,
                    Vector3.Zero,
                    new Vector3(0, -tongueBackDorsal, 0));
            }

            if (_tongueTip != null)
            {
                var start = GetPose(_tongueTip);

                _tongueTipInterdental = CreateBlend(
                    start,
                    Vector3.Zero,
                    new Vector3(0, -tongueInterdental, 0));

                _tongueTipAlveolar = CreateBlend(
                    start,
                    Vector3.Zero,
                    new Vector3(0, -tongueAlveolar, 0));
            }
        }

        protected override void Update(RenderContext ctx)
        {
            _xrApp ??= XrApp.Current;

            if (_xrApp == null || !_xrApp.IsStarted)
                return;

            if (_xrApp.SessionState != SessionState.Focused)
                return;

            if (_faceTrack == null)
            {
                _faceTrack = new XrFaceTrack(_xrApp);
                _faceTrack.Create();
            }

            if (_morph == null || _targets == null)
                return;

            var weights = _faceTrack.GetWeigths();

            if (weights == null || !_faceTrack.IsValid)
                return;

            // Clear only weights owned by this component, including disabled approximations.
            foreach (var name in _morphWeights.Keys)
                _morph.Weights[_targets[name]] = 0;
            _morphWeights.Clear();

            UpdateDirectMorphs(weights);

            if (UseApproximateMorphs)
            {
                UpdateApproximateMorphs(weights);
                UpdateCorrectiveMorphs();
            }

            UpdateSkin(weights);

            _morph.InvalidateWeights();
        }

        private void UpdateDirectMorphs(XrFaceWeight[] weights)
        {
            Set("anim_browLowerer_L", weights, FaceExpression2FB.BrowLowererLFB);
            Set("anim_browLowerer_R", weights, FaceExpression2FB.BrowLowererRFB);

            Set("anim_cheekPuff_L", weights, FaceExpression2FB.CheekPuffLFB);
            Set("anim_cheekPuff_R", weights, FaceExpression2FB.CheekPuffRFB);

            Set("anim_cheekRaiser_L", weights, FaceExpression2FB.CheekRaiserLFB);
            Set("anim_cheekRaiser_R", weights, FaceExpression2FB.CheekRaiserRFB);

            Set("anim_dimpler_L", weights, FaceExpression2FB.DimplerLFB);
            Set("anim_dimpler_R", weights, FaceExpression2FB.DimplerRFB);

            Set("anim_eyesClosed_L", weights, FaceExpression2FB.EyesClosedLFB);
            Set("anim_eyesClosed_R", weights, FaceExpression2FB.EyesClosedRFB);

            Set("anim_eyesLookDown_L", weights, FaceExpression2FB.EyesLookDownLFB);
            Set("anim_eyesLookDown_R", weights, FaceExpression2FB.EyesLookDownRFB);
            Set("anim_eyesLookLeft_L", weights, FaceExpression2FB.EyesLookLeftLFB);
            Set("anim_eyesLookLeft_R", weights, FaceExpression2FB.EyesLookLeftRFB);
            Set("anim_eyesLookRight_L", weights, FaceExpression2FB.EyesLookRightLFB);
            Set("anim_eyesLookRight_R", weights, FaceExpression2FB.EyesLookRightRFB);
            Set("anim_eyesLookUp_L", weights, FaceExpression2FB.EyesLookUpLFB);
            Set("anim_eyesLookUp_R", weights, FaceExpression2FB.EyesLookUpRFB);

            Set("anim_innerBrowRaiser_L", weights, FaceExpression2FB.InnerBrowRaiserLFB);
            Set("anim_innerBrowRaiser_R", weights, FaceExpression2FB.InnerBrowRaiserRFB);

            Set("anim_jawDrop", weights, FaceExpression2FB.JawDropFB);
            Set("anim_jawSidewaysLeft", weights, FaceExpression2FB.JawSidewaysLeftFB);
            Set("anim_jawSidewaysRight", weights, FaceExpression2FB.JawSidewaysRightFB);
            Set("anim_jawThrust", weights, FaceExpression2FB.JawThrustFB);

            Set("anim_lidTightener_L", weights, FaceExpression2FB.LidTightenerLFB);
            Set("anim_lidTightener_R", weights, FaceExpression2FB.LidTightenerRFB);

            Set("anim_lipCornerDepressor_L", weights, FaceExpression2FB.LipCornerDepressorLFB);
            Set("anim_lipCornerDepressor_R", weights, FaceExpression2FB.LipCornerDepressorRFB);

            Set("anim_lipCornerPuller_L", weights, FaceExpression2FB.LipCornerPullerLFB);
            Set("anim_lipCornerPuller_R", weights, FaceExpression2FB.LipCornerPullerRFB);

            Set("anim_lipPucker_L", weights, FaceExpression2FB.LipPuckerLFB);
            Set("anim_lipPucker_R", weights, FaceExpression2FB.LipPuckerRFB);

            Set("anim_lipStretcher_L", weights, FaceExpression2FB.LipStretcherLFB);
            Set("anim_lipStretcher_R", weights, FaceExpression2FB.LipStretcherRFB);

            Set("anim_lowerLipDepressor_L", weights, FaceExpression2FB.LowerLipDepressorLFB);
            Set("anim_lowerLipDepressor_R", weights, FaceExpression2FB.LowerLipDepressorRFB);

            Set("anim_mouthLeft", weights, FaceExpression2FB.MouthLeftFB);
            Set("anim_mouthRight", weights, FaceExpression2FB.MouthRightFB);

            Set("anim_noseWrinkler_L", weights, FaceExpression2FB.NoseWrinklerLFB);
            Set("anim_noseWrinkler_R", weights, FaceExpression2FB.NoseWrinklerRFB);

            Set("anim_outerBrowRaiser_L", weights, FaceExpression2FB.OuterBrowRaiserLFB);
            Set("anim_outerBrowRaiser_R", weights, FaceExpression2FB.OuterBrowRaiserRFB);

            Set("anim_upperLidRaiser_L", weights, FaceExpression2FB.UpperLidRaiserLFB);
            Set("anim_upperLidRaiser_R", weights, FaceExpression2FB.UpperLidRaiserRFB);

            Set("anim_upperLipRaiser_L", weights, FaceExpression2FB.UpperLipRaiserLFB);
            Set("anim_upperLipRaiser_R", weights, FaceExpression2FB.UpperLipRaiserRFB);
        }

        private void UpdateApproximateMorphs(XrFaceWeight[] weights)
        {
            // The legacy avatar splits each chin control into left and right halves.
            Set("anim_chinRaiser_LT", weights, FaceExpression2FB.ChinRaiserTFB);
            Set("anim_chinRaiser_RT", weights, FaceExpression2FB.ChinRaiserTFB);
            Set("anim_chinRaiser_LB", weights, FaceExpression2FB.ChinRaiserBFB);
            Set("anim_chinRaiser_RB", weights, FaceExpression2FB.ChinRaiserBFB);

            Set("anim_lipPressor",
                MathF.Max(Get(weights, FaceExpression2FB.LipPressorLFB), Get(weights, FaceExpression2FB.LipPressorRFB)));

            Set("anim_lipTightener",
                MathF.Max(Get(weights, FaceExpression2FB.LipTightenerLFB), Get(weights, FaceExpression2FB.LipTightenerRFB)));

            Set("anim_lipSuck_T",
                MathF.Max(Get(weights, FaceExpression2FB.LipSuckLTFB), Get(weights, FaceExpression2FB.LipSuckRTFB)));

            Set("anim_lipSuck_B",
                MathF.Max(Get(weights, FaceExpression2FB.LipSuckLBFB), Get(weights, FaceExpression2FB.LipSuckRBFB)));

            Set("anim_lipFunneler_L",
                MathF.Max(Get(weights, FaceExpression2FB.LipFunnelerLTFB), Get(weights, FaceExpression2FB.LipFunnelerLBFB)));

            Set("anim_lipFunneler_R",
                MathF.Max(Get(weights, FaceExpression2FB.LipFunnelerRTFB), Get(weights, FaceExpression2FB.LipFunnelerRBFB)));

            var lipsToward = Get(weights, FaceExpression2FB.LipsTowardFB);

            Set("anim_lipsToward_LT", lipsToward);
            Set("anim_lipsToward_RT", lipsToward);
            Set("anim_lipsToward_LB", lipsToward);
            Set("anim_lipsToward_RB", lipsToward);

        }

        private void UpdateSkin(XrFaceWeight[] weights)
        {
            if (_eyeLeft != null)
            {
                var position = _eyeLeftUp.Start.Position;
                var orientation = _eyeLeftUp.Start.Orientation;

                AddBlend(ref position, ref orientation, _eyeLeftUp,
                    Get(weights, FaceExpression2FB.EyesLookUpLFB));

                AddBlend(ref position, ref orientation, _eyeLeftDown,
                    Get(weights, FaceExpression2FB.EyesLookDownLFB));

                AddBlend(ref position, ref orientation, _eyeLeftLeft,
                    Get(weights, FaceExpression2FB.EyesLookLeftLFB));

                AddBlend(ref position, ref orientation, _eyeLeftRight,
                    Get(weights, FaceExpression2FB.EyesLookRightLFB));

                SetPose(_eyeLeft, position, orientation);
            }

            if (_eyeRight != null)
            {
                var position = _eyeRightUp.Start.Position;
                var orientation = _eyeRightUp.Start.Orientation;

                AddBlend(ref position, ref orientation, _eyeRightUp,
                    Get(weights, FaceExpression2FB.EyesLookUpRFB));

                AddBlend(ref position, ref orientation, _eyeRightDown,
                    Get(weights, FaceExpression2FB.EyesLookDownRFB));

                AddBlend(ref position, ref orientation, _eyeRightLeft,
                    Get(weights, FaceExpression2FB.EyesLookLeftRFB));

                AddBlend(ref position, ref orientation, _eyeRightRight,
                    Get(weights, FaceExpression2FB.EyesLookRightRFB));

                SetPose(_eyeRight, position, orientation);
            }

            if (_dentureBottom != null)
            {
                var position = _jawDrop.Start.Position;
                var orientation = _jawDrop.Start.Orientation;

                AddBlend(ref position, ref orientation, _jawDrop,
                    Get(weights, FaceExpression2FB.JawDropFB));

                AddBlend(ref position, ref orientation, _jawLeft,
                    Get(weights, FaceExpression2FB.JawSidewaysLeftFB));

                AddBlend(ref position, ref orientation, _jawRight,
                    Get(weights, FaceExpression2FB.JawSidewaysRightFB));

                AddBlend(ref position, ref orientation, _jawThrust,
                    Get(weights, FaceExpression2FB.JawThrustFB));

                SetPose(_dentureBottom, position, orientation);
            }

            if (_tongueBase != null)
            {
                var position = _tongueOut.Start.Position;
                var orientation = _tongueOut.Start.Orientation;

                AddBlend(ref position, ref orientation, _tongueOut,
                    Get(weights, FaceExpression2FB.TongueOutFB));

                AddBlend(ref position, ref orientation, _tongueRetreat,
                    Get(weights, FaceExpression2FB.TongueRetreatFB));

                AddBlend(ref position, ref orientation, _tongueFrontDorsal,
                    Get(weights, FaceExpression2FB.TongueFrontDorsalPalateFB));

                AddBlend(ref position, ref orientation, _tongueMidDorsal,
                    Get(weights, FaceExpression2FB.TongueMidDorsalPalateFB));

                AddBlend(ref position, ref orientation, _tongueBackDorsal,
                    Get(weights, FaceExpression2FB.TongueBackDorsalVelarFB));

                SetPose(_tongueBase, position, orientation);
            }

            if (_tongueTip != null)
            {
                var position = _tongueTipInterdental.Start.Position;
                var orientation = _tongueTipInterdental.Start.Orientation;

                AddBlend(ref position, ref orientation, _tongueTipInterdental,
                    Get(weights, FaceExpression2FB.TongueTipInterdentalFB));

                AddBlend(ref position, ref orientation, _tongueTipAlveolar,
                    Get(weights, FaceExpression2FB.TongueTipAlveolarFB));

                SetPose(_tongueTip, position, orientation);
            }
        }

        private static JointBlend CreateBlend(Pose3 start, Vector3 position, Vector3 rotation)
        {
            return new JointBlend
            {
                Start = start,
                End = new Pose3
                {
                    Position = start.Position + position,
                    Orientation = Quaternion.Normalize(
                        start.Orientation *
                        Quaternion.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z))
                }
            };
        }

        private static void AddBlend(
            ref Vector3 position,
            ref Quaternion orientation,
            JointBlend blend,
            float weight)
        {
            if (weight <= 0)
                return;

            weight = Math.Clamp(weight, 0, 1);

            position += (blend.End.Position - blend.Start.Position) * weight;

            var endDelta = Quaternion.Normalize(
                Quaternion.Inverse(blend.Start.Orientation) *
                blend.End.Orientation);

            var delta = Quaternion.Slerp(Quaternion.Identity, endDelta, weight);

            orientation = Quaternion.Normalize(orientation * delta);
        }

        private static Pose3 GetPose(Joint3D joint)
        {
            return new Pose3
            {
                Position = joint.Transform.Position,
                Orientation = joint.Transform.Orientation
            };
        }

        private static void SetPose(Joint3D joint, Vector3 position, Quaternion orientation)
        {
            joint.Transform.Position = position;
            joint.Transform.Orientation = orientation;
            joint.Transform.Update();
        }

        private void Set(string name, XrFaceWeight[] weights, FaceExpression2FB expression)
        {
            Set(name, Get(weights, expression));
        }

        private void Set(string name, float value)
        {
            if (_targets!.TryGetValue(name, out var index))
            {
                _morph!.Weights[index] = value;
                _morphWeights[name] = value;
            }
        }

        private static float Get(XrFaceWeight[] weights, FaceExpression2FB expression)
        {
            var value = weights[(int)expression].Weight;
            return float.IsFinite(value) ? Math.Clamp(value, 0, 1) : 0;
        }
    }
}
