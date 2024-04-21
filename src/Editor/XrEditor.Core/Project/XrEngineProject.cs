using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine;

namespace XrEditor
{
    public class XrEngineProject
    {
        string? _basePath;

        public XrEngineProject()
        {
            Current = this;
        }

        public void Create(string basePath)
        {

        }


        public void Load(string basePath)
        {

        }

        public void Import(IAsset asset)
        {
        }


        public string? BasePath => _basePath;


        public static XrEngineProject? Current { get; private set; }
    }
}
