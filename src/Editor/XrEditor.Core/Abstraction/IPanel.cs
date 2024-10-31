﻿namespace XrEditor
{

    [AttributeUsage(AttributeTargets.Class)]
    public class PanelAttribute : Attribute
    {
        public PanelAttribute(string panelId)
        {
            PanelId = panelId;
        }
        public string PanelId { get; }
    }   


    public interface IPanel
    {
        Task CloseAsync();

        string PanelId { get; } 

        string? Title { get; }

        ToolbarView? ToolBar { get; }
    }
}
