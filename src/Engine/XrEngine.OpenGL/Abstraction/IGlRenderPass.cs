using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.OpenGL
{
    public interface IGlRenderPass
    {
        void RenderContent(GlobalContent content);

        bool IsEnabled { get; set; }
    }
}
