
using System.Numerics;
using static XrEngine.Bullet.BulletLib;

namespace XrEngine.Bullet
{
    public static class IkBodies
    {
        public static IkNode CreateArm()
        {
            // Axes
            Vector3 X = Vector3.UnitX;
            Vector3 Y = Vector3.UnitY;
            Vector3 Z = Vector3.UnitZ;

            float zero = 0.001f;

            Vector3 pelvisPos = new Vector3(0.0f, 0.9f, 0.0f);
            Vector3 spine1Pos = pelvisPos + new Vector3(0, 0.20f, 0);
            Vector3 spine2Pos = spine1Pos + new Vector3(0, 0.05f, 0); ;
            Vector3 headEffPos = spine2Pos + new Vector3(0, 0.10f, 0);

            Vector3 lShoulderPos = spine2Pos + new Vector3(-0.2f, 0, 0);
            Vector3 lShoulderPitchPos = lShoulderPos + new Vector3(-0.02f, 0, 0);
            Vector3 lUpArmPos = lShoulderPitchPos + new Vector3(-0.21f, 0, 0);
            Vector3 lLowArmPos = lUpArmPos + new Vector3(-0.21f, 0, 0);
            Vector3 lHandPos = lLowArmPos + new Vector3(-0.10f, 0, 0);

            IkNode pelvis = Joint(pelvisPos, X, 0.10f, -10, 10, 0, "Pelvis");
            IkNode spine1 = Joint(spine1Pos, X, 0.10f, -10, 10, 0, "Spine1");
            IkNode spine2 = Joint(spine2Pos, X, 0.10f, -10, 10, 0, "Spine2");
            IkNode headEff = Effector(headEffPos, "Head");
            IkNode lShoulder = Joint(lShoulderPos, Y, 0.10f, -90, 0, 0, "Shoulder-L");
            IkNode lShoulderPitch = Joint(lShoulderPitchPos, -Z, 0.10f, -90, 90, 0, "Shoulder-Pitch-L");
            IkNode lUpArm = Joint(lUpArmPos, Y, 0.10f, -90, 0, 0, "UpArm-L");
            IkNode lLowArm = Joint(lLowArmPos, Y, 0.10f, -90, 0, 0, "LowArm-L");
            IkNode lHand = Effector(lHandPos, "Hand");

            // Spine chain
            pelvis.Left = spine1;
            spine1.Left = spine2;
            spine2.Right = headEff;
            spine2.Left = lShoulder;
            lShoulder.Left = lShoulderPitch;
            lShoulderPitch.Left = lUpArm;
            lUpArm.Left = lLowArm;
            lLowArm.Left = lHand;

            return pelvis;
        }

        public static IkNode CreateArms()
        {
            Vector3 X = Vector3.UnitX;
            Vector3 Y = Vector3.UnitY;
            Vector3 Z = Vector3.UnitZ;

            const float SHOULDER_SIZE = 0.22f;
            const float LOW_ARM_SIZE = 0.24f;
            const float UP_ARM_SIZE = 0.24f;

            Vector3 pelvisPos = new Vector3(0.0f, 0.9f, 0.0f);
            Vector3 spine1Pos = pelvisPos + new Vector3(0, 0.20f, 0);
            Vector3 spine2Pos = spine1Pos + new Vector3(0, 0.05f, 0);
            Vector3 spine3Pos = spine2Pos;
            Vector3 headEffPos = spine3Pos + new Vector3(0, 0.10f, 0);

            // ----- Left arm (unchanged)
            Vector3 lShoulderPos = spine2Pos + new Vector3(-SHOULDER_SIZE, 0, 0);
            Vector3 lShoulderPitchPos = lShoulderPos + new Vector3(-0.02f, 0, 0);
            Vector3 lUpArmPos = lShoulderPitchPos + new Vector3(-UP_ARM_SIZE, 0, 0);
            Vector3 lLowArmPos = lUpArmPos + new Vector3(-LOW_ARM_SIZE, 0, 0);
            Vector3 lHandPos = lLowArmPos + new Vector3(-0.02f, 0, 0);

            // ----- Right arm (mirrored)
            Vector3 rShoulderPos = spine2Pos + new Vector3(SHOULDER_SIZE, 0, 0);
            Vector3 rShoulderPitchPos = rShoulderPos + new Vector3(0.02f, 0, 0);
            Vector3 rUpArmPos = rShoulderPitchPos + new Vector3(UP_ARM_SIZE, 0, 0);
            Vector3 rLowArmPos = rUpArmPos + new Vector3(LOW_ARM_SIZE, 0, 0);
            Vector3 rHandPos = rLowArmPos + new Vector3(0.02f, 0, 0);

            // ----- Spine
            IkNode pelvis = Joint(pelvisPos, Y, 0.10f, -30, 30, 0, "Pelvis");
            IkNode spine1 = Joint(spine1Pos, X, 0.10f, -10, 10, 0, "Spine1");
            IkNode spine2 = Joint(spine2Pos, X, 0.10f, -10, 10, 0, "Spine2");
            IkNode spine3 = Joint(spine3Pos, X, 0.10f, -10, 10, 0, "Spine3");
            IkNode headEff = Effector(headEffPos, "Head");

            // ----- Left arm joints
            IkNode lShoulder = Joint(lShoulderPos, Y, 0.10f, -90, 0, 0, "Shoulder-L");
            IkNode lShoulderPitch = Joint(lShoulderPitchPos, -Z, 0.10f, -90, 90, 0, "Shoulder-Pitch-L");
            IkNode lUpArm = Joint(lUpArmPos, Y, 0.10f, -90, 0, 0, "UpArm-L");
            IkNode lLowArm = Joint(lLowArmPos, Y, 0.10f, -90, 30, 0, "LowArm-L");
            IkNode lHand = Effector(lHandPos, "Hand-L");

            // ----- Right arm joints (mirrored)
            IkNode rShoulder = Joint(rShoulderPos, Y, 0.10f, 0, 90, 0, "Shoulder-R");
            IkNode rShoulderPitch = Joint(rShoulderPitchPos, Z, 0.10f, -90, 90, 0, "Shoulder-Pitch-R");
            IkNode rUpArm = Joint(rUpArmPos, Y, 0.10f, 0, 90, 0, "UpArm-R");
            IkNode rLowArm = Joint(rLowArmPos, Y, 0.10f, -30, 90, 0, "LowArm-R");
            IkNode rHand = Effector(rHandPos, "Hand-R");

            // ----- Hierarchy
            pelvis.Left = spine1;
            spine1.Left = spine2;
            spine2.Left = spine3;
            spine3.Left = headEff;

            // left
            spine2.Right = lShoulder;
            lShoulder.Left = lShoulderPitch;
            lShoulderPitch.Left = lUpArm;
            lUpArm.Left = lLowArm;
            lLowArm.Left = lHand;

            // right
            spine3.Right = rShoulder;       // use your second child slot name
            rShoulder.Left = rShoulderPitch;
            rShoulderPitch.Left = rUpArm;
            rUpArm.Left = rLowArm;
            rLowArm.Left = rHand;

            return pelvis;
        }

        static IkNode CreateHuman()
        {
            // Axes
            Vector3 X = new(1, 0, 0);
            Vector3 Y = new(0, 1, 0);
            Vector3 Z = new(0, 0, 1);

            // ---------------------------------------------------------
            // BASE POSITIONS
            // ---------------------------------------------------------
            Vector3 pelvisPos = new Vector3(0.0f, 0.9f, 0.0f);

            Vector3 spine1Pos = pelvisPos;                           // rotational only
            Vector3 spine2Pos = spine1Pos + new Vector3(0, 0.10f, 0);
            Vector3 spine3Pos = spine2Pos + new Vector3(0, 0.10f, 0);

            Vector3 chestPos = spine3Pos + new Vector3(0, 0.05f, 0);
            Vector3 headPos = chestPos + new Vector3(0, 0.15f, 0);
            Vector3 headEffPos = headPos + new Vector3(0, 0.10f, 0);

            // HIPS / LEGS
            Vector3 lHipBase = pelvisPos + new Vector3(-0.10f, -0.05f, 0);
            Vector3 rHipBase = pelvisPos + new Vector3(0.10f, -0.05f, 0);

            Vector3 lUpperLegPos = lHipBase + new Vector3(0, -0.20f, 0);
            Vector3 lLowerLegPos = lUpperLegPos + new Vector3(0, -0.20f, 0);

            Vector3 rUpperLegPos = rHipBase + new Vector3(0, -0.20f, 0);
            Vector3 rLowerLegPos = rUpperLegPos + new Vector3(0, -0.20f, 0);

            // CLAVICLES / SHOULDERS / ARMS
            Vector3 lClavicleBase = chestPos + new Vector3(-0.07f, 0.05f, 0);
            Vector3 rClavicleBase = chestPos + new Vector3(0.07f, 0.05f, 0);

            Vector3 lShoulderPos = lClavicleBase + new Vector3(-0.03f, 0.02f, 0);
            Vector3 rShoulderPos = rClavicleBase + new Vector3(0.03f, 0.02f, 0);

            Vector3 lUpperArmPos = lShoulderPos + new Vector3(-0.18f, 0, 0);
            Vector3 lLowerArmPos = lUpperArmPos + new Vector3(-0.22f, 0, 0);
            Vector3 lHandPos = lLowerArmPos + new Vector3(-0.05f, 0, 0);

            Vector3 rUpperArmPos = rShoulderPos + new Vector3(0.18f, 0, 0);
            Vector3 rLowerArmPos = rUpperArmPos + new Vector3(0.22f, 0, 0);
            Vector3 rHandPos = rLowerArmPos + new Vector3(0.05f, 0, 0);

            // ---------------------------------------------------------
            // CREATE NODES
            // ---------------------------------------------------------

            // ROOT & SPINE
            IkNode pelvis = Joint(pelvisPos, Y, 0.10f, -45, 45, 0, "Pelvis");

            IkNode spine1 = Joint(spine1Pos, X, 0.10f, -10, 10, 0, "Spine1");
            IkNode spine2 = Joint(spine2Pos, X, 0.10f, -10, 10, 0, "Spine2");
            IkNode spine3 = Joint(spine3Pos, X, 0.10f, -10, 10, 0, "Spine3");

            IkNode chest = Joint(chestPos, Y, 0.10f, -20, 20, 0, "Chest");

            // HEAD
            IkNode head = Joint(headPos, X, 0.05f, -30, 30, 0, "Head");
            IkNode headEff = Effector(headEffPos, "Head_Eff");

            // LEFT LEG
            IkNode lHipSwing = Joint(lHipBase, Z, 0.01f, -25, 25, 0, "L_Hip_Swing"); // abduction/adduction
            IkNode lHipFlex = Joint(lHipBase, X, 0.05f, -45, 60, 0, "L_Hip");       // flex/ext
            IkNode lUpperLeg = Joint(lUpperLegPos, X, 0.40f, 0, 110, 20, "L_Upper_Leg");
            IkNode lLowerLeg = Joint(lLowerLegPos, X, 0.40f, 0, 130, 60, "L_Lower_Leg");

            // RIGHT LEG
            IkNode rHipSwing = Joint(rHipBase, -Z, 0.01f, -25, 25, 0, "R_Hip_Swing");
            IkNode rHipFlex = Joint(rHipBase, X, 0.05f, -45, 60, 0, "R_Hip");
            IkNode rUpperLeg = Joint(rUpperLegPos, X, 0.40f, 0, 110, 20, "R_Upper_Leg");
            IkNode rLowerLeg = Joint(rLowerLegPos, X, 0.40f, 0, 130, 60, "R_Lower_Leg");

            // CLAVICLES
            IkNode lClavicle = Joint(lClavicleBase, Z, 0.05f, -20, 20, 0, "L_Clavicle");   // elevate/depress
            IkNode rClavicle = Joint(rClavicleBase, -Z, 0.05f, -20, 20, 0, "R_Clavicle");

            // LEFT ARM (ball-approx: yaw + pitch)
            IkNode lShoulderYaw = Joint(lShoulderPos, Y, 0.05f, -80, 80, 0, "L_Shoulder_Yaw");
            IkNode lShoulderPitch = Joint(lShoulderPos, Z, 0.05f, -10, 120, 0, "L_Shoulder_Pitch");

            IkNode lUpperArm = Joint(lUpperArmPos, Z, 0.35f, -10, 135, 40, "L_Upper_Arm");
            IkNode lLowerArm = Joint(lLowerArmPos, Z, 0.35f, 0, 140, 80, "L_Lower_Arm");
            IkNode lHand = Effector(lHandPos, "L_Hand");

            // RIGHT ARM (mirror)
            IkNode rShoulderYaw = Joint(rShoulderPos, Y, 0.05f, -80, 80, 0, "R_Shoulder_Yaw");
            IkNode rShoulderPitch = Joint(rShoulderPos, -Z, 0.05f, -10, 120, 0, "R_Shoulder_Pitch");

            IkNode rUpperArm = Joint(rUpperArmPos, -Z, 0.35f, -10, 135, 40, "R_Upper_Arm");
            IkNode rLowerArm = Joint(rLowerArmPos, -Z, 0.35f, 0, 140, 80, "R_Lower_Arm");
            IkNode rHand = Effector(rHandPos, "R_Hand");

            // ---------------------------------------------------------
            // LC/RS TREE
            // ---------------------------------------------------------

            // Spine chain
            pelvis.Left = spine1;
            spine1.Left = spine2;
            spine2.Left = spine3;
            spine3.Left = chest;

            // Legs: attach to Spine1 as in your old code
            spine1.Right = lHipSwing;

            // Left leg chain
            lHipSwing.Left = lHipFlex;
            lHipFlex.Left = lUpperLeg;
            lUpperLeg.Left = lLowerLeg;
            // (optionally add a foot joint/effector later)

            // Right leg as sibling of left leg
            lHipSwing.Right = rHipSwing;
            rHipSwing.Left = rHipFlex;
            rHipFlex.Left = rUpperLeg;
            rUpperLeg.Left = rLowerLeg;

            // Chest children: Head first
            chest.Left = head;
            head.Left = headEff;

            // Left clavicle as sibling of head
            head.Right = lClavicle;
            lClavicle.Left = lShoulderYaw;
            lShoulderYaw.Left = lShoulderPitch;
            lShoulderPitch.Left = lUpperArm;
            lUpperArm.Left = lLowerArm;
            lLowerArm.Left = lHand;

            // Right clavicle as sibling of left clavicle
            lClavicle.Right = rClavicle;
            rClavicle.Left = rShoulderYaw;
            rShoulderYaw.Left = rUpperArm;
            //rShoulderYaw.Left = rShoulderPitch;
            //rShoulderPitch.Left = rUpperArm;
            rUpperArm.Left = rLowerArm;
            rLowerArm.Left = rHand;

            return pelvis;
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

