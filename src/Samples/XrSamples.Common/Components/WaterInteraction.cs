using OpenXr.Framework;
using Silk.NET.OpenXR;
using System.Numerics;
using XrEngine;
using XrEngine.OpenXr;
using XrMath;

namespace XrSamples
{
    public sealed class WaterInteraction : Behavior<Water>
    {
        private const int LeftPalmSlot = 0;
        private const int LeftFingerSlot = 1;
        private const int RightPalmSlot = 2;
        private const int RightFingerSlot = 3;

        private enum ContactSource
        {
            None,
            Hand,
            Controller
        }

        public struct Contact
        {
            public Vector3 LocalPosition;

            public Vector3 PreviousLocalPosition;

            public float Radius;

            public float Speed;
        }

        public sealed class WaterInteractionState
        {
            public readonly Contact[] Contacts = new Contact[4];

            public Object3D Player = null!;

            public Vector2 PlayerLocalPosition;

            public Vector3 PlayerPosition;

            public float PlayerMotion;

            public float PlayerDistance;

            public bool PlayerTeleported;

            public float LeftHandMotion;

            public Vector3 LeftHandPosition;

            public float RightHandMotion;

            public Vector3 RightHandPosition;
        }

        private readonly WaterInteractionState _state = new();
        private WaterMaterial _material = null!;
        private readonly Vector3[] _lastContacts = new Vector3[4];
        private readonly ContactSource[] _contactSources = new ContactSource[4];
        private XrPoseInput? _leftGripPose;
        private XrPoseInput? _rightGripPose;
        private Vector2 _lastPlayerPosition;
        private Vector2 _walkingOrigin;
        private bool _hasPlayerPosition;
        private bool _isWalking;
        private float _idleTime;

        public WaterInteraction()
        {
            UpdatePriority = 1;
            PlayerDisturbanceRadius = 0.16f;
            PlayerDisturbanceStrength = 5f;
            WalkingThreshold = 0.15f;
        }

        protected override void Start(RenderContext ctx)
        {
            _material = _host.Material;
            _state.Player = _host.Scene!.DescendantsOrSelfComponents<XrPlayer>().Single().Host!;
            base.Start(ctx);
        }

        protected override void Update(RenderContext ctx)
        {
            var deltaTime = (float)ctx.DeltaTime;

            if (deltaTime <= 0)
            {
                _state.PlayerMotion = 0;
                _state.PlayerDistance = 0;
                _state.LeftHandMotion = 0;
                _state.RightHandMotion = 0;
                Array.Clear(_state.Contacts);
                return;
            }

            UpdatePlayer(deltaTime);
            UpdateHands(deltaTime);
            UpdateHandMotion(LeftPalmSlot, LeftFingerSlot, out var leftMotion, out var leftPosition);
            UpdateHandMotion(RightPalmSlot, RightFingerSlot, out var rightMotion, out var rightPosition);
            _state.LeftHandMotion = leftMotion;
            _state.LeftHandPosition = leftPosition;
            _state.RightHandMotion = rightMotion;
            _state.RightHandPosition = rightPosition;
        }

        private void UpdateHands(float deltaTime)
        {
            var app = XrApp.Current;

            if (app == null || !app.IsStarted)
            {
                Array.Clear(_state.Contacts);
                Array.Clear(_contactSources);
                return;
            }

            _leftGripPose ??= (XrPoseInput)app.Inputs["LeftGripPose"];
            _rightGripPose ??= (XrPoseInput)app.Inputs["RightGripPose"];

            UpdateHand(app, HandEXT.LeftExt, _leftGripPose, LeftPalmSlot, LeftFingerSlot, deltaTime);
            UpdateHand(app, HandEXT.RightExt, _rightGripPose, RightPalmSlot, RightFingerSlot, deltaTime);
        }

        private void UpdateHand(XrApp app, HandEXT side, XrPoseInput gripPose,
            int palmSlot, int fingerSlot, float deltaTime)
        {
            app.Hands.TryGetValue(side, out var hand);

            if (hand != null && hand.IsActive && hand.Joints != null)
            {
                var palm = hand.Joints[(int)HandJointEXT.PalmExt];
                var finger = hand.Joints[(int)HandJointEXT.IndexTipExt];
                var referenceFrame = app.ReferenceFrame.ToMatrix();

                UpdateJoint(palm, referenceFrame, palmSlot, 0.055f, deltaTime);
                UpdateJoint(finger, referenceFrame, fingerSlot, 0.025f, deltaTime);
                return;
            }

            ClearContact(fingerSlot);
            UpdateController(app, gripPose, palmSlot, deltaTime);
        }

        private void UpdateJoint(HandJointLocationEXT joint, Matrix4x4 referenceFrame,
            int slot, float radius, float deltaTime)
        {
            if ((joint.LocationFlags & SpaceLocationFlags.PositionValidBit) == 0)
            {
                ClearContact(slot);
                return;
            }

            var world = Vector3.Transform(joint.Pose.Position.ToVector3(), referenceFrame);
            UpdateContact(slot, world, radius, ContactSource.Hand, deltaTime);
        }

        private void UpdateController(XrApp app, XrPoseInput gripPose, int slot, float deltaTime)
        {
            if (!gripPose.IsActive)
            {
                ClearContact(slot);
                return;
            }

            var location = app.SpacesTracker.GetLastLocation(gripPose.Space);

            if (location == null || !location.IsValid)
            {
                ClearContact(slot);
                return;
            }

            UpdateContact(slot, location.Pose.Position, 0.055f, ContactSource.Controller, deltaTime);
        }

        private void ClearContact(int slot)
        {
            _state.Contacts[slot] = default;
            _contactSources[slot] = ContactSource.None;
        }

        private void UpdateContact(int slot, Vector3 world, float radius, ContactSource source, float deltaTime)
        {
            var local = _host.ToLocal(world);
            var previous = _lastContacts[slot];
            var distance = Vector3.Distance(local, previous);
            var continuous = source == _contactSources[slot] && distance < 0.5f && deltaTime < 0.1f;
            var speed = 0f;

            if (continuous)
                speed = Math.Clamp(distance / MathF.Max(deltaTime, 0.0001f), 0f, 2f);
            else
            {
                previous = local;
                radius = 0f;
            }

            _state.Contacts[slot] = new Contact
            {
                LocalPosition = local,
                PreviousLocalPosition = previous,
                Radius = radius,
                Speed = speed
            };
            _lastContacts[slot] = local;
            _contactSources[slot] = source;
        }

        private void UpdatePlayer(float deltaTime)
        {
            var local = _host.ToLocal(_state.Player.WorldPosition);
            var position = new Vector2(local.X, local.Y);
            var size = Vector2.Max(_material.WaterSize, new Vector2(0.001f));
            var distance = Vector2.Distance(position, _lastPlayerPosition);
            var isInside = MathF.Abs(position.X) <= size.X * 0.5f &&
                MathF.Abs(position.Y) <= size.Y * 0.5f && _material.WaterDepth > 0;
            var isContinuous = _hasPlayerPosition && distance < 0.5f && deltaTime < 0.1f;
            var speed = distance / deltaTime;

            _state.PlayerLocalPosition = position;
            _state.PlayerPosition = _host.ToWorld(new Vector3(position, 0));
            _state.PlayerMotion = 0;
            _state.PlayerDistance = 0;
            _state.PlayerTeleported = _hasPlayerPosition && !isContinuous;

            if (!isContinuous || !isInside)
            {
                _walkingOrigin = position;
                _isWalking = false;
                _idleTime = 0;
            }
            else if (speed >= 0.12f)
            {
                _idleTime = 0;

                if (!_isWalking && Vector2.Distance(position, _walkingOrigin) >= WalkingThreshold)
                    _isWalking = true;

                if (_isWalking)
                {
                    _state.PlayerMotion = Math.Clamp(speed / 1.2f, 0f, 1f);
                    _state.PlayerDistance = distance;
                }
            }
            else
            {
                _idleTime += deltaTime;

                if (_idleTime >= 0.2f)
                {
                    _walkingOrigin = position;
                    _isWalking = false;
                }
            }

            _lastPlayerPosition = position;
            _hasPlayerPosition = true;
        }

        private void UpdateHandMotion(int palmSlot, int fingerSlot, out float motion, out Vector3 position)
        {
            motion = ContactMotion(palmSlot, out position);
            var fingerMotion = ContactMotion(fingerSlot, out var fingerPosition);

            if (fingerMotion > motion)
            {
                motion = fingerMotion;
                position = fingerPosition;
            }
        }

        private float ContactMotion(int slot, out Vector3 position)
        {
            var contact = _state.Contacts[slot];
            position = default;

            if (contact.Radius <= 0 || contact.Speed < 0.015f || _material.WaterDepth <= 0)
                return 0;

            var a = contact.PreviousLocalPosition;
            var b = contact.LocalPosition;
            var t = 1f;

            if (MathF.Abs(b.Z - a.Z) > 0.0001f)
                t = Math.Clamp(-a.Z / (b.Z - a.Z), 0f, 1f);

            var local = Vector3.Lerp(a, b, t);
            var halfSize = _material.WaterSize * 0.5f;
            var depth = MathF.Abs(local.Z) / contact.Radius;

            if (depth >= 2f || MathF.Abs(local.X) > halfSize.X || MathF.Abs(local.Y) > halfSize.Y)
                return 0;

            var fade = Math.Clamp(depth - 1f, 0f, 1f);
            fade = 1f - fade * fade * (3f - 2f * fade);
            position = _host.ToWorld(new Vector3(local.X, local.Y, 0));
            return contact.Speed * fade;
        }

        public override void Reset(bool onlySelf = false)
        {
            _hasPlayerPosition = false;
            _isWalking = false;
            _idleTime = 0;
            _state.PlayerMotion = 0;
            _state.PlayerDistance = 0;
            _state.PlayerTeleported = true;
            _state.LeftHandMotion = 0;
            _state.RightHandMotion = 0;
            Array.Clear(_state.Contacts);
            Array.Clear(_contactSources);
            base.Reset(onlySelf);
        }

        public WaterInteractionState State => _state;

        public float PlayerDisturbanceRadius { get; set; }

        public float PlayerDisturbanceStrength { get; set; }

        public float WalkingThreshold { get; set; }
    }
}
