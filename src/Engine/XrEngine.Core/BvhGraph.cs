using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine
{
    public class BvhNode<T> where T : Object3D
    {
        public BvhNode(T value)
        {
            Bounds = value.WorldBounds;
            Value = value;
        }

        public BvhNode(BvhNode<T> left, BvhNode<T> right)
        {
            Left = left;
            Right = right;
            Bounds = left.Bounds.Merge(right.Bounds);

            Left.Parent = this;
            Right.Parent = this;
        }

        public Bounds3 Bounds;

        public BvhNode<T>? Left;

        public BvhNode<T>? Right;

        public BvhNode<T>? Parent;

        public T? Value;

        [MemberNotNullWhen(true, nameof(Value))]
        [MemberNotNullWhen(false, nameof(Left), nameof(Right))]
        public bool IsLeaf => Left == null && Right == null;
    }



    public class BvhGraph<T> where T : Object3D
    {
        public BvhGraph(IEnumerable<T> objects)
        {
            Root = Build(objects.ToList());
        }

        private static BvhNode<T> Build(List<T> objects)
        {
            if (objects.Count == 1)
                return new BvhNode<T>(objects[0]);

            // Find the longest axis and sort objects by their centroid along this axis
            Vector3 min = Vector3.Zero, max = Vector3.Zero;
            foreach (var obj in objects)
            {
                min = Vector3.Min(min, obj.WorldBounds.Min);
                max = Vector3.Max(max, obj.WorldBounds.Max);
            }
            Vector3 size = max - min;
            int axis = (size.X > size.Y && size.X > size.Z) ? 0 : (size.Y > size.Z ? 1 : 2);

            objects = objects.OrderBy(o => o.WorldBounds.Min[axis]).ToList();

            // Split objects into two groups and recurse
            int mid = objects.Count / 2;
            var left = Build(objects.Take(mid).ToList());
            var right = Build(objects.Skip(mid).ToList());

            return new BvhNode<T>(left, right);
        }

        public List<T> Query(Bounds3 range)
        {
            var results = new List<T>();
            QueryRecursive(Root, range, results);
            return results;
        }

        private static void QueryRecursive(BvhNode<T>? node, Bounds3 range, IList<T> results)
        {
            if (node == null || !node.Bounds.Intersects(range))
                return;

            if (node.IsLeaf)
            {
                if (range.Intersects(node.Bounds))
                    results.Add(node.Value!);
            }
            else
            {
                QueryRecursive(node.Left, range, results);
                QueryRecursive(node.Right, range, results);
            }
        }

        public T? FindClosest(Vector3 point)
        {
            return FindClosestRecursive(Root, point, out _);
        }

        private static T? FindClosestRecursive(BvhNode<T>? node, Vector3 point, out float closestDist)
        {
            if (node == null)
            {
                closestDist = float.MaxValue;
                return null;
            }

            closestDist = node.Bounds.DistanceTo(point);

            if (node.IsLeaf)
                return node.Value;

            var leftDist = node.Left != null ? node.Left.Bounds.DistanceTo(point) : float.MaxValue;
            var rightDist = node.Right != null ? node.Right.Bounds.DistanceTo(point) : float.MaxValue;

            var first = leftDist < rightDist ? node.Left : node.Right;
            var second = leftDist < rightDist ? node.Right : node.Left;

            var closestObject = FindClosestRecursive(first, point, out float firstDist);
            closestDist = firstDist;

            if (second != null && second.Bounds.DistanceTo(point) < closestDist)
            {
                var secondClosest = FindClosestRecursive(second, point, out float secondDist);
                if (secondDist < closestDist)
                {
                    closestObject = secondClosest;
                    closestDist = secondDist;
                }
            }

            return closestObject;
        }

        public void Insert(T value)
        {
            var newNode = new BvhNode<T>(value);

            if (Root == null)
            {
                Root = newNode;
            }
            else
            {
                // Find the best place to insert the new node
                BvhNode<T> current = Root!;
                while (!current.IsLeaf)
                {
                    var enlargementLeft = current.Left.Bounds.Merge(value.WorldBounds).Volume() - current.Left.Bounds.Volume();
                    var enlargementRight = current.Right.Bounds.Merge(value.WorldBounds).Volume() - current.Right.Bounds.Volume();

                    current = (enlargementLeft < enlargementRight) ? current.Left : current.Right;
                }

                // Create a new parent node
                var oldParent = current.Parent;
                var newParent = new BvhNode<T>(current, newNode);
                newParent.Parent = oldParent;

                if (oldParent == null)
                {
                    Root = newParent;
                }
                else
                {
                    if (oldParent.Left == current)
                        oldParent.Left = newParent;
                    else
                        oldParent.Right = newParent;

                    UpdateBoundingBoxesUpwards(oldParent);
                }
            }
        }

        public void Remove(T obj)
        {
            var node = FindNode(Root, obj);
            if (node == null) 
                return; 

            var parent = node.Parent;
            if (parent == null)
            {
                Root = null; 
                return;
            }

            var sibling = (parent.Left == node ? parent.Right : parent.Left)!;

            if (parent.Parent == null)
            {
                Root = sibling;
                sibling.Parent = null;
            }
            else
            {

                var grandParent = parent.Parent;
                if (grandParent.Left == parent)
                    grandParent.Left = sibling;
                else
                    grandParent.Right = sibling;

                sibling.Parent = grandParent;
                UpdateBoundingBoxesUpwards(grandParent);
            }
        }

        private static void UpdateBoundingBoxesUpwards(BvhNode<T>? node)
        {
            while (node != null)
            {
                node.Bounds = node.Left!.Bounds.Merge(node.Right!.Bounds);
                node = node.Parent;
            }
        }

        private static BvhNode<T>? FindNode(BvhNode<T>? node, T obj)
        {
            if (node == null) 
                return null;

            if (node.IsLeaf && node.Value == obj) 
                return node;

            var foundInLeft = FindNode(node.Left, obj);
            return foundInLeft ?? FindNode(node.Right, obj);
        }

        public BvhNode<T>? Root { get; private set; }   
    }
}