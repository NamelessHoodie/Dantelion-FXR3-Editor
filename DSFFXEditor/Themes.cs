using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using ImGuiNET;

namespace DSFFXEditor
{
    class DSFFXThemes
    {
        public static void ThemesSelector(String themeName)
        {
            if (themeName == "DarkRedClay")
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.00f, 1.00f)); //Pretty Self explanatory
                //ImGui.PushStyleColor(ImGuiCol.TextDisabled, new Vector4(0.60f, 1.0f, 0.60f, 1.00f)); //idk yet
                ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.17f, 0.16f, 0.16f, 1.0f)); //Window Background
                ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(0.35f, 0.24f, 0.24f, 1.00f)); //Popup Background
                ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.61f, 0.50f, 0.50f, 0.90f)); //Context Menu Border
                ImGui.PushStyleColor(ImGuiCol.BorderShadow, new Vector4(0.00f, 0.00f, 0.00f, 0.39f)); //idk
                ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.37f, 0.30f, 0.30f, 0.50f)); // Not clicked/hovered Control Background
                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.42f, 0.30f, 0.30f, 0.60f)); // Hovered Control Background
                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.46f, 0.30f, 0.30f, 0.70f)); // Clicked Control Background
                ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.5f, 0.39f, 0.39f, 0.50f)); // Unselected window title color
                ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, new Vector4(1.00f, 1.00f, 1.00f, 0.51f)); // Collapsed window title color
                ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.5f, 0.39f, 0.39f, 0.85f)); // Selected window title color
                ImGui.PushStyleColor(ImGuiCol.MenuBarBg, new Vector4(0.5f, 0.39f, 0.39f, 0.50f)); // MenuBar color
                ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, new Vector4(0.5f, 0.39f, 0.39f, 0.50f)); // Scroll bar Background
                ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, new Vector4(0.5f, 0.39f, 0.39f, 1.00f)); // Scroll bar Grabby bit color
                ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabHovered, new Vector4(0.55f, 0.39f, 0.39f, 1.00f)); // Scroll bar grabby bit Hover
                ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabActive, new Vector4(0.60f, 0.39f, 0.39f, 1.00f)); // Scroll bar color when clicked
                ImGui.PushStyleColor(ImGuiCol.CheckMark, new Vector4(0.5f, 0.39f, 0.39f, 1.00f)); // Checkbox Sign Color
                ImGui.PushStyleColor(ImGuiCol.SliderGrab, new Vector4(0.5f, 0.39f, 0.39f, 1.00f)); // Sliders grabby bit color
                ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, new Vector4(0.60f, 0.39f, 0.39f, 1.00f)); // Sliders grabby bit color when clicked
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.37f, 0.30f, 0.30f, 1.00f)); //Button Control Color Overrides
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.42f, 0.30f, 0.30f, 0.94f)); //Button Control Color Overrides
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.46f, 0.30f, 0.30f, 0.94f)); //Button Control Color Overrides
                ImGui.PushStyleColor(ImGuiCol.DockingPreview, new Vector4(0.00f, 0.00f, 0.00f, 0.67f)); //Docking screen preview color
                //ImGui.PushStyleColor(ImGuiCol.DockingEmptyBg, new Vector4(0.00f, 0.00f, 0.00f, 0.39f));
                ImGui.PushStyleColor(ImGuiCol.Tab, new Vector4(0.35f, 0.30f, 0.30f, 0.90f)); //Unfocused Tab Color?
                ImGui.PushStyleColor(ImGuiCol.TabHovered, new Vector4(0.00f, 0.00f, 0.00f, 0.39f)); //Window Tab Color when hovered
                ImGui.PushStyleColor(ImGuiCol.TabActive, new Vector4(0.00f, 0.00f, 0.00f, 0.39f)); //Focused Window Tab color
                ImGui.PushStyleColor(ImGuiCol.TabUnfocused, new Vector4(0.00f, 0.00f, 0.00f, 0.39f));
                ImGui.PushStyleColor(ImGuiCol.TabUnfocusedActive, new Vector4(0.35f, 0.30f, 0.30f, 0.50f)); //Unfocused Window Tab color
                ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.24f, 0.15f, 0.15f, 0.60f)); //Menubar/context bar clicked tab?
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.45f, 0.2f, 0.2f, 0.80f)); //Menubar/context bar Hovered Tab
                //ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.26f, 0.59f, 0.98f, 1.00f));
                ImGui.PushStyleColor(ImGuiCol.ResizeGrip, new Vector4(0.5f, 0.39f, 0.39f, 1.00f));
                ImGui.PushStyleColor(ImGuiCol.ResizeGripHovered, new Vector4(0.55f, 0.39f, 0.39f, 1.00f));
                ImGui.PushStyleColor(ImGuiCol.ResizeGripActive, new Vector4(0.60f, 0.39f, 0.39f, 1.00f));
                //ImGui.PushStyleColor(ImGuiCol.PlotLines, new Vector4(0.39f, 0.39f, 0.39f, 1.00f));
                //ImGui.PushStyleColor(ImGuiCol.PlotLinesHovered, new Vector4(1.00f, 0.43f, 0.35f, 1.00f));
                //ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(0.90f, 0.70f, 0.00f, 1.00f));
                //ImGui.PushStyleColor(ImGuiCol.PlotHistogramHovered, new Vector4(1.00f, 0.60f, 0.00f, 1.00f));
                //ImGui.PushStyleColor(ImGuiCol.TextSelectedBg, new Vector4(0.26f, 0.59f, 0.98f, 0.35f)); // Most Likely Selected Text
                ImGui.PushStyleColor(ImGuiCol.Separator, new Vector4(0.31f, 0.31f, 0.31f, 1.00f));
                ImGui.PushStyleColor(ImGuiCol.TableBorderLight, new Vector4(0.31f, 0.31f, 0.31f, 0.70f));
                ImGui.PushStyleColor(ImGuiCol.TableBorderStrong, new Vector4(0.31f, 0.31f, 0.31f, 1.00f));
                ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, new Vector4(0.36f, 0.26f, 0.26f, 1.00f));
            }
            else if (themeName == "ImGuiDark")
            {
                ImGui.StyleColorsDark();
            }
            else if (themeName == "ImGuiLight")
            {
                ImGui.StyleColorsLight();
            }
            else if (themeName == "ImGuiClassic")
            {
                ImGui.StyleColorsClassic();
            }
        }
    }
}
