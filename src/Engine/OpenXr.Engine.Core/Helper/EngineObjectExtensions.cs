﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public static class EngineObjectExtensions
    {
       public static Behavior<T> AddBehavior<T>(this T obj, Action<T, RenderContext> action) where T : EngineObject   
        {
            var result = new LambdaBehavior<T>(action);
            obj.AddComponent(result);
            return result;
        }

        public static IEnumerable<Group> Ancestors(this Object3D obj)
        {
            var curItem = obj.Parent;

            while (curItem != null)
            {
                yield return curItem;
                curItem = curItem.Parent;
            }
        }

        public static Group? FindAncestor<T>(this Object3D obj) where T : Group
        {
            return obj.Ancestors().OfType<T>().FirstOrDefault();
        }

    }
}
