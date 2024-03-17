﻿using CanvasUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine.UI
{
    public class WindowManager3D : Behavior<Scene3D>, IUiWindowManager
    {
        public WindowManager3D() 
        {
            UiManager.WindowManager = this; 
        }

        public IUiWindow CreateWindow(Size2 size, Vector3 position, UiElement content)
        {
            var result = new Window3D();

            _host!.AddChild(result);

            result.Size = size; 
            result.WorldPosition = position;
            result.Content = content;

            return result;
        }
    }
}
