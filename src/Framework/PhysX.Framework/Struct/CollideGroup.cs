namespace PhysX.Framework
{
    public enum CollideGroupMode
    {
        Collide,
        NotCollide,
        Check
    }

    public class CollideGroup
    {
        public CollideGroup(Func<PhysicsRigidActor, PhysicsRigidActor, bool> check)
        {
            Check = check;
        }


        public readonly Func<PhysicsRigidActor, PhysicsRigidActor, bool> Check;

        public static CollideGroup Always = new((_, _) => true);

        public static CollideGroup Never = new((_, _) => false);
    }
}
