﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEditor
{
    public interface IEditorProperties
    {
        void EditorProperties(IList<PropertyView> curProps);

        public bool AutoGenerate { get; set; }
    }
}
