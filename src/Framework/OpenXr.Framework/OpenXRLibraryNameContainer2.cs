using Silk.NET.Core.Loader;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenXr.Framework
{
    public class OpenXRLibraryNameContainer2 : SearchPathContainer
    {
        /// <inheritdoc />
        public override string[] Linux => new[] { "libopenxr_loader.so" };

        /// <inheritdoc />
        public override string[] MacOS => new[] { "null" };

        /// <inheritdoc />
        public override string[] Android => new[] { "libopenxr_loader.so" };

        /// <inheritdoc />
        public override string[] IOS => new[] { "__Internal" };

        /// <inheritdoc />
        public override string[] Windows64 => new[] { "openxr_loader.dll" };

        /// <inheritdoc />
        public override string[] Windows86 => new[] { "openxr_loader.dll" };
    }
}
