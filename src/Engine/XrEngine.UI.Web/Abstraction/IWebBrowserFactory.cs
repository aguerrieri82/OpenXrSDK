using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.UI.Web
{

    public interface IWebBrowserFactory
    {
        IWebBrowser CreateBrowser(TriangleMesh destMesh);
    }
}
