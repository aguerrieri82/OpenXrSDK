using System;
using System.Collections.Generic;

using System.Numerics;
using XrEngine;
using XrEngine.Bullet;
using XrMath;
using static Tensorflow.ApiDef.Types;
using static XrEngine.Bullet.BulletLib;

namespace XrSamples.Components
{
    public class IkViewer : Behavior<Group3D>, IDrawGizmos
    {
        IkSolver _solver;
        Object3D?[] _targets = new Object3D?[3];

        public IkViewer()
        {
            _solver = new IkSolver();
            _solver.Build(CreateHuman());
        }

        public void SetTarget(int index, Object3D? obj)
        {
            _targets[index] = obj;  
        }

        protected override void Update(RenderContext ctx)
        {
            int i = 0;

            foreach (var effector in _solver.Effectors)
            {
                if (_targets[i] != null)
                    _solver.SetTarget(effector, _targets[i]!.WorldPosition);
                i++;
            }

            _solver.Update(Method, true);

        }

        Matrix4x4 GetLocalTransform(IkNode node)
        {
            var axis = node.Axis;

            if (axis.LengthSquared() > 0f)
                axis = Vector3.Normalize(axis);
            else
                axis = Vector3.Zero;

            var theta = node.Theta;

            Quaternion rot = axis == Vector3.Zero
                ? Quaternion.Identity
                : Quaternion.CreateFromAxisAngle(axis, theta);

            return Matrix4x4.CreateFromQuaternion(rot) *
                   Matrix4x4.CreateTranslation(node.RelPos);

        }

        bool _log = true;

        void DrawWork(Canvas3D canvas, IkNode node, Matrix4x4 tr,  Matrix4x4 parentTr)
        {

            if (node == null)
                return;

            // Current world position
            var pos = tr.Translation;

            if (_log)
                Log.Info(this, "{0}: {1}", node.Name, pos);

            // Draw basis axes
            var bx = new Vector3(tr.M11, tr.M12, tr.M13);
            var by = new Vector3(tr.M21, tr.M22, tr.M23);
            var bz = new Vector3(tr.M31, tr.M32, tr.M33);

            canvas.State.Color = new Color(1, 0, 0, 1);  // X
            canvas.DrawLine(pos, pos + bx * 0.05f);

            canvas.State.Color = new Color(0, 1, 0, 1);  // Y
            canvas.DrawLine(pos, pos + by * 0.05f);

            canvas.State.Color = new Color(0, 0, 1, 1);  // Z
            canvas.DrawLine(pos, pos + bz * 0.05f);

            // Draw rotation axis (local axis → world space)
            var axisLocal = node.Axis;
            var axisWorld = Vector3.TransformNormal(axisLocal, tr);

            canvas.State.Color = new Color(0.2f, 0.2f, 0.7f, 1);
            canvas.DrawLine(pos, pos + axisWorld * 0.1f);


            if (node.Right != null)
            {
                var act = GetLocalTransform(node.Right);

                var trSibling = act * parentTr;

                canvas.State.Color = new Color(0, 1, 0, 1); // green
                canvas.DrawLine(pos, trSibling.Translation);

                DrawWork(canvas, node.Right, trSibling, parentTr);
            }

            if (node.Left != null)
            {
                var act = GetLocalTransform(node.Left);
                var trChild = act * tr;

                canvas.State.Color = new Color(1, 0, 0, 1); // red
                canvas.DrawLine(pos, trChild.Translation);

                DrawWork(canvas, node.Left, trChild, tr);
            }
        }

        public void DrawGizmos(Canvas3D canvas)
        {
            DrawWork(canvas, _solver.Root!, GetLocalTransform(_solver.Root!), Matrix4x4.Identity);

            _log = false;
        }

        public IkSolver Solver => _solver;

        static IkNode CreateHuman()
        {
            // Axes
            Vector3 X = new(1, 0, 0);
            Vector3 Y = new(0, 1, 0);
            Vector3 Z = new(0, 0, 1);

            // Base positions
            var pelvisPos = new Vector3(0.0f, 0.9f, 0.0f);

            var spine1Pos = pelvisPos + new Vector3(0, 0.1f, 0);
            var spine2Pos = spine1Pos + new Vector3(0, 0.1f, 0);
            var spine3Pos = spine2Pos + new Vector3(0, 0.1f, 0);

            var lHipBase = pelvisPos + new Vector3(-0.1f, -0.05f, 0);
            var rHipBase = pelvisPos + new Vector3(0.1f, -0.05f, 0);

            var lShoulderBase = spine3Pos + new Vector3(-0.15f, 0.05f, 0);
            var rShoulderBase = spine3Pos + new Vector3(0.15f, 0.05f, 0);

            var lUpperLegPos = lHipBase + new Vector3(0, -0.2f, 0);
            var lLowerLegPos = lUpperLegPos + new Vector3(0, -0.2f, 0);

            var rUpperLegPos = rHipBase + new Vector3(0, -0.2f, 0);
            var rLowerLegPos = rUpperLegPos + new Vector3(0, -0.2f, 0);

            var lUpperArmPos = lShoulderBase + new Vector3(-0.2f, 0, 0);
            var lLowerArmPos = lUpperArmPos + new Vector3(-0.2f, 0, 0);

            var rUpperArmPos = rShoulderBase + new Vector3(0.2f, 0, 0);
            var rLowerArmPos = rUpperArmPos + new Vector3(0.2f, 0, 0);

            var lHandPos = lLowerArmPos + new Vector3(-0.05f, 0, 0);
            var rHandPos = rLowerArmPos + new Vector3(0.05f, 0, 0);

            // ======================================================
            // CREATE ALL NODES WITH NAMES
            // ======================================================

            // Root & Spine
            var pelvis = Joint(pelvisPos, Y, 0.10f, -45, 45, 0, "Pelvis");
  
            var spine1 = Joint(spine1Pos, Y, 0.10f, -10, 10, 0, "Spine1");
            var spine2 = Joint(spine2Pos, X, 0.10f, -10, 10, 0, "Spine2");
            var spine3 = Joint(spine3Pos, X, 0.10f, -10, 10, 0, "Spine3");


            // LEFT LEG
            var lHipSwing = Joint(lHipBase, Z, 0.01f, -25, 25, 0, "L_Hip_Swing");
            var lHipFlex = Joint(lHipBase, X, 0.05f, -45, 60, 0, "L_Hip");
            var lUpperLeg = Joint(lUpperLegPos, X, 0.40f, 0, 110, 20, "L_Upper_Leg");
            var lLowerLeg = Joint(lLowerLegPos, X, 0.40f, 0, 130, 60, "L_Lower_Leg");

            // RIGHT LEG
            var rHipSwing = Joint(rHipBase, -Z, 0.01f, -25, 25, 0, "R_Hip_Swing");
            var rHipFlex = Joint(rHipBase, X, 0.05f, -45, 60, 0, "R_Hip");
            var rUpperLeg = Joint(rUpperLegPos, X, 0.40f, 0, 110, 20, "R_Upper_Leg");
            var rLowerLeg = Joint(rLowerLegPos, X, 0.40f, 0, 130, 60, "R_Lower_Leg");

            // LEFT ARM
            var lShoulderYaw = Joint(lShoulderBase, Y, 0.05f, -90, 90, 0, "L_Shoulder_Yaw");
            var lShoulderPitch = Joint(lShoulderBase, X, 0.01f, -60, 60, 0, "L_Shoulder_Pitch");

            var lUpperArm = Joint(lUpperArmPos, Z, 0.35f, 0, 140, 60, "L_Upper_Arm");
            var lLowerArm = Joint(lLowerArmPos, Z, 0.35f, 0, 140, 80, "L_Lower_Arm");
            var lHand = Effector(lHandPos, "L_Hand");

            // RIGHT ARM
            var rShoulderYaw = Joint(rShoulderBase, Y, 0.05f, -90, 90, 0, "R_Shoulder_Yaw");
            var rShoulderPitch = Joint(rShoulderBase, -X, 0.01f, -60, 60, 0, "R_Shoulder_Pitch");

            var rUpperArm = Joint(rUpperArmPos, -Z, 0.35f, 0, 140, 60, "R_Upper_Arm");
            var rLowerArm = Joint(rLowerArmPos, -Z, 0.35f, 0, 140, 80, "R_Lower_Arm");
            var rHand = Effector(rHandPos, "R_Hand");

            // ======================================================
            // BUILD LC/RS TREE (EXACT 1:1 MATCH TO C++ BULLET CODE)
            // ======================================================

            // Pelvis → Spine chain
            pelvis.Left = spine1;
            spine1.Left = spine2;
            spine2.Left = spine3;

            // LEFT LEG
            spine1.Right = lHipSwing;
            lHipSwing.Left = lHipFlex;
            lHipFlex.Left = lUpperLeg;
            lUpperLeg.Left = lLowerLeg;

            // RIGHT LEG
            lHipSwing.Right = rHipSwing;
            rHipSwing.Left = rHipFlex;
            rHipFlex.Left = rUpperLeg;
            rUpperLeg.Left = rLowerLeg;

            // LEFT ARM
            spine3.Left = lShoulderYaw;
            lShoulderYaw.Left = lShoulderPitch;
            lShoulderPitch.Left = lUpperArm;
            lUpperArm.Left = lLowerArm;
            lLowerArm.Left = lHand;

            // RIGHT ARM
            lShoulderYaw.Right = rShoulderYaw;
            rShoulderYaw.Left = rShoulderPitch;
            rShoulderPitch.Left = rUpperArm;
            rUpperArm.Left = rLowerArm;
            rLowerArm.Left = rHand;

            return pelvis; // root
        }

        public IkUpdateMethod Method { get; set; }

        public float ArmTeta
        {
            get => _solver.FindNode("L_Upper_Arm").Theta;
            set => _solver.FindNode("L_Upper_Arm").Theta = value;
        }


        static IkNode Joint(Vector3 attach, Vector3 axis, float size, float min, float max, float rest, string name)
        {
            return new IkNode
            {
                Attach = attach,
                Axis = axis,
                Size = size,
                Purpose = Purpose.Joint,
                MinTheta = MathF.PI * min / 180f,
                MaxTheta = MathF.PI * max / 180f,
                RestAngle = MathF.PI * rest / 180f,
                //Theta = MathF.PI * rest / 180f,
                Name = name
            };
        }

        static IkNode Effector(Vector3 attach, string name)
        {
            return new IkNode
            {
                Attach = attach,
                Axis = Vector3.UnitY,
                Size = 0.01f,
                Purpose = Purpose.Effector,
                MinTheta = 0,
                MaxTheta = 0,
                RestAngle = 0,
                Name = name
            };
        }
    }
}

