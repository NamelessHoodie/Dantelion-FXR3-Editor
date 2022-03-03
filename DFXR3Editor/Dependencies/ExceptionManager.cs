using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace DFXR3Editor.Dependencies
{
    public static class ExceptionManager
    {
        public class RenderableException
        {
            public string Title { get; }
            public string ExceptionSummary { get; }
            public RenderableException(string title, Exception exception)
            {
                Title = title;
                ExceptionSummary = exception.ToString();
            }
            public void Render()
            {
                PushExceptionForRendering(this);
            }
        }

        private static bool _isRenderingTopException = false;
        private static RenderableException _currentException = null;
        public static void PushExceptionForRendering(RenderableException exception)
        {
            _isRenderingTopException = true;
            _currentException = exception;
        }
        public static void PushExceptionForRendering(string title, Exception exception)
        {
            var newException = new RenderableException(title, exception);
            PushExceptionForRendering(newException);
        }

        public static bool TryRenderException()
        {
            if (!_isRenderingTopException)
                return false;

            RenderException(_currentException);
            return true;
        }

        private static void RenderException(RenderableException exception)
        {
            string exceptionTitle = exception.Title;
            string exceptionSummary = exception.ExceptionSummary;
            if (!ImGui.IsPopupOpen(exceptionTitle))
            {
                ImGui.OpenPopup(exceptionTitle);
            }
            if (ImGui.IsPopupOpen(exceptionTitle))
            {
                Vector2 textInputSize = SetExceptionScreenPositionAndSize();
                if (ImGui.BeginPopupModal(exceptionTitle, ref _isRenderingTopException, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize))
                {
                    bool escapePressed = ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Escape));

                    ImGui.InputTextMultiline("TextInput", ref exceptionSummary, 1024, textInputSize, ImGuiInputTextFlags.ReadOnly);
                    ImGui.EndPopup();
                    if (escapePressed)
                    {
                        _isRenderingTopException = false;
                    }
                }
            }
        }

        private static Vector2 SetExceptionScreenPositionAndSize()
        {
            ImGuiViewportPtr mainViewport = ImGui.GetMainViewport();
            Vector2 textInputSize = new Vector2(mainViewport.Size.X * 0.8f, mainViewport.Size.Y * 0.8f);
            ImGui.SetNextWindowPos(new Vector2(mainViewport.Pos.X + mainViewport.Size.X * 0.5f, mainViewport.Pos.Y + mainViewport.Size.Y * 0.5f), ImGuiCond.Always, new Vector2(0.5f, 0.5f));
            return textInputSize;
        }
    }
}
