using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using ImGuiNET;

namespace ImGuiNETAddons
{
    public static class ImGuiAddons
    {
        public static void ToggleButton(string str_id, ref bool v)
        {
            Vector2 p = ImGuiNET.ImGui.GetCursorScreenPos();
            ImDrawListPtr draw_list = ImGuiNET.ImGui.GetWindowDrawList();

            float height = ImGuiNET.ImGui.GetFrameHeight();
            float width = height * 1.55f;
            float radius = height * 0.50f;

            if (ImGuiNET.ImGui.InvisibleButton(str_id, new Vector2(width, height)))
            {
                v = !v;
            }
            uint col_bg;
            uint col_Nub;
            if (ImGuiNET.ImGui.IsItemHovered())
            {
                col_bg = v ? ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonHovered) : ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonActive, 0.5f);
                col_Nub = v ? ImGuiNET.ImGui.GetColorU32(ImGuiCol.ScrollbarGrabActive) : ImGuiNET.ImGui.GetColorU32(ImGuiCol.ScrollbarGrabActive);
            }
            else
            {
                col_bg = v ? ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonHovered, 0.8f) : ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonHovered, 0.5f);
                col_Nub = v ? ImGuiNET.ImGui.GetColorU32(ImGuiCol.ScrollbarGrabHovered) : ImGuiNET.ImGui.GetColorU32(ImGuiCol.ScrollbarGrabHovered);
            }
            draw_list.AddRectFilled(p, new Vector2(p.X + width, p.Y + height), col_bg, height * 0.5f);
            draw_list.AddCircleFilled(new Vector2(v ? (p.X + width - radius) : (p.X + radius), p.Y + radius), radius - 1.5f, col_Nub);
        }

        public static bool ButtonGradient(string str_id)
        {
            String[] str_idArray = str_id.Split("###");
            Vector2 p = ImGuiNET.ImGui.GetCursorScreenPos();
            Vector2 sizeText = ImGui.CalcTextSize(str_idArray[0]);
            ImDrawListPtr draw_list = ImGuiNET.ImGui.GetWindowDrawList();
            float ButtonHeight = ImGuiNET.ImGui.GetFrameHeight(); //Dynamically Allocated Height
            float ButtonWidth = sizeText.X + sizeText.X*0.20f; //Dynamically Allocated Width
            Vector2 ButtonSize = new Vector2(p.X + ButtonWidth, p.Y + ButtonHeight);
            uint col_Top;
            uint col_Bottom;
            if (str_idArray.Length > 1)
            {
                if (ImGuiNET.ImGui.InvisibleButton(str_idArray[1], new Vector2(ButtonWidth, ButtonHeight)))
                {
                    return true;
                }
            }
            else
            {
                if (ImGuiNET.ImGui.InvisibleButton(str_idArray[0], new Vector2(ButtonWidth, ButtonHeight)))
                {
                    return true;
                }
            }
            if (ImGuiNET.ImGui.IsItemHovered())
            {
                col_Top = ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonActive);
                col_Bottom = ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonHovered);
            }
            else
            {
                col_Top = ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonHovered);
                col_Bottom = ImGuiNET.ImGui.GetColorU32(ImGuiCol.Button);
            }
            draw_list.AddRectFilledMultiColor(p, ButtonSize, col_Top, col_Top, col_Bottom, col_Bottom);
            draw_list.AddRect(p, ButtonSize, ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonActive));
            draw_list.AddText(new Vector2(p.X + (ButtonWidth / 2) - (sizeText.X / 2), p.Y + (ButtonHeight / 2) - (sizeText.Y / 2)), ImGui.GetColorU32(ImGuiCol.Text), str_idArray[0]);
            return false;
        }
        public static bool ButtonGradient(string str_id, Vector2 ButtonSizeVector)
        {
            String[] str_idArray = str_id.Split("###");
            Vector2 p = ImGuiNET.ImGui.GetCursorScreenPos();
            Vector2 sizeText = ImGui.CalcTextSize(str_idArray[0]);
            ImDrawListPtr draw_list = ImGuiNET.ImGui.GetWindowDrawList();
            //ButtonHeight += ImGuiNET.ImGui.GetFrameHeight(); //Dynamically Allocated Height
            //float ButtonWidth = sizeText.X + ButtonHeight; //Dynamically Allocated Width
            float ButtonHeight = ButtonSizeVector.Y; //Fixed Height
            float ButtonWidth = ButtonSizeVector.X; //Dynamically Allocated Width
            if (ButtonWidth < sizeText.X)
            {
                ButtonWidth = sizeText.X + sizeText.X*0.20f;
            }
            Vector2 ButtonSize = new Vector2(p.X + ButtonWidth, p.Y + ButtonHeight);
            uint col_Top;
            uint col_Bottom;
            if (str_idArray.Length > 1)
            {
                if (ImGuiNET.ImGui.InvisibleButton(str_idArray[1], new Vector2(ButtonWidth, ButtonHeight)))
                {
                    return true;
                }
            }
            else
            {
                if (ImGuiNET.ImGui.InvisibleButton(str_idArray[0], new Vector2(ButtonWidth, ButtonHeight)))
                {
                    return true;
                }
            }
            if (ImGuiNET.ImGui.IsItemHovered())
            {
                col_Top = ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonActive);
                col_Bottom = ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonHovered);
            }
            else
            {
                col_Top = ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonHovered);
                col_Bottom = ImGuiNET.ImGui.GetColorU32(ImGuiCol.Button);
            }
            draw_list.AddRectFilledMultiColor(p, ButtonSize, col_Top, col_Top, col_Bottom, col_Bottom);
            draw_list.AddRect(p, ButtonSize, ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonActive));
            draw_list.AddText(new Vector2(p.X + (ButtonWidth / 2) - (sizeText.X / 2), p.Y + (ButtonHeight / 2) - (sizeText.Y/2)), ImGui.GetColorU32(ImGuiCol.Text), str_idArray[0]);
            return false;
        }
    }
}
