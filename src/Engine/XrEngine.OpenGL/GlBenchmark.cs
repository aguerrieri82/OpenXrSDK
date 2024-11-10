#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.OpenGL
{
    public class GlBenchmark
    {
        readonly GL _gl;

        public GlBenchmark(GL gl)
        {
            _gl = gl;
        }

        public void Bench()
        {
            DrawBufferMode[] a = [DrawBufferMode.ColorAttachment0, DrawBufferMode.ColorAttachment1];
            DrawBufferMode[] b = [DrawBufferMode.ColorAttachment0, DrawBufferMode.ColorAttachment1];

            Bench(10000000,
                () => Standard(a),
                () => CompareA(a, b),
                () => CompareB(a, b),
                () => CompareC(a, b),
                () => CompareD(a, b),
                () => CompareF(a, b));
        }


        public void Bench(int iterations, params Action[] actions)
        {
            int task = 0;
            foreach (var action in actions)
            {
                Log.Info(this, "Running Task: {0}", task);
                Debug.WriteLine("Running Task: {0}", task);
                
                var time = Stopwatch.GetTimestamp();

                for (var i = 0; i < iterations; i++)
                    action();
                
                var diff = Stopwatch.GetElapsedTime(time);

                Log.Info(this, "Task: {0}, {1} ms", task, diff.TotalMilliseconds);
                Debug.WriteLine("Task: {0}, {1} ms", task, diff.TotalMilliseconds);
                task++;
            }
        }

        public void Standard(DrawBufferMode[] buffer)
        {
             _gl.DrawBuffers(buffer);
        }

        public bool CompareA<T>(T[] a, T[] b)
        {
            return a.AsSpan().SequenceEqual(b); 
        }

        public bool CompareB<T>(T[] a, T[] b) where T: struct
        {
            var len = a.Length;
            if (len != b.Length)
                return false;

            for (var i = 0; i < len; i++)   
                if (!a[i].Equals(b[i]) == false)
                    return false;
            return true;
        }

        public unsafe bool CompareC<T>(T[] a, T[] b) where T : struct
        {
            var len = a.Length;
            if (len != b.Length)
                return false;

            var size = sizeof(T);
            fixed (T* pa = a, pb = b)
                return EngineNativeLib.CompareMemory((nint)pa, (nint)pb, (uint)(len * size)) == 0;
        }

        public unsafe bool CompareD<T>(T[] a, T[] b) where T : struct
        {
            var len = a.Length;
            if (len != b.Length)
                return false;

            var nint = len * sizeof(T) / 4;
            fixed (T* pa = a, pb = b)
            {
                var intA = (int*)pa;
                var intB = (int*)pb;
                while (nint > 0)
                {
                    if (*intA != *intB)
                        return false;
                    intA++;
                    intB++;
                    nint--;
                }
            }
            return true;
        }

        public bool CompareF(DrawBufferMode[] a, DrawBufferMode[] b) 
        {
            var len = a.Length;
            if (len != b.Length)
                return false;

            for (var i = 0; i < len; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }
    }
}
