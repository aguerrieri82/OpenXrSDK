using System;
using System.Collections.Generic;
using System.Text;
using XrEngine;

namespace XrEditor
{
    public class ComponentPreset
    {
        public string? Name;

        public IStateContainer? State;
    }

    public interface IPresetManager
    {
        void SavePreset(string name);

        IList<ComponentPreset> ListPresets();

        void LoadPreset(ComponentPreset preset);

        void DeletePreset(ComponentPreset preset);
    }
}
