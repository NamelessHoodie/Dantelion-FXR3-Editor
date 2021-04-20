using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using ImGuiNET;

namespace ImGuiNETAddons
{
    public static class ImGuiAddons
    {
        public static bool ToggleButton(string str_id, ref bool v)
        {
            Vector2 p = ImGuiNET.ImGui.GetCursorScreenPos();
            ImDrawListPtr draw_list = ImGuiNET.ImGui.GetWindowDrawList();
            bool isClicked = false;

            float height = ImGuiNET.ImGui.GetFrameHeight();
            float width = height * 1.55f;
            float radius = height * 0.50f;

            if (ImGuiNET.ImGui.InvisibleButton(str_id, new Vector2(width, height)))
            {
                isClicked = true;
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
            return isClicked;
        }

        public static bool ButtonGradient(string str_id)
        {
            String[] str_idArray = str_id.Split("##");
            Vector2 p = ImGuiNET.ImGui.GetCursorScreenPos();
            Vector2 sizeText = ImGui.CalcTextSize(str_idArray[0]);
            ImDrawListPtr draw_list = ImGuiNET.ImGui.GetWindowDrawList();
            float ButtonHeight = ImGuiNET.ImGui.GetFrameHeight();
            float ButtonWidth = sizeText.X + sizeText.X * 0.20f;
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
                col_Top = ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonHovered, 1.50f);
                col_Bottom = ImGuiNET.ImGui.GetColorU32(ImGuiCol.Button, 0.50f);
            }
            else
            {
                col_Top = ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonHovered);
                col_Bottom = ImGuiNET.ImGui.GetColorU32(ImGuiCol.Button, 0.20f);
            }
            draw_list.AddRectFilledMultiColor(p, ButtonSize, col_Top, col_Top, col_Bottom, col_Bottom);
            draw_list.AddRect(p, ButtonSize, ImGuiNET.ImGui.GetColorU32(ImGuiCol.Separator));
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
                ButtonWidth = sizeText.X + sizeText.X * 0.20f;
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
            draw_list.AddText(new Vector2(p.X + (ButtonWidth / 2) - (sizeText.X / 2), p.Y + (ButtonHeight / 2) - (sizeText.Y / 2)), ImGui.GetColorU32(ImGuiCol.Text), str_idArray[0]);
            return false;
        }
        public static Vector2 CalcItemSize(Vector2 size, float default_w, float default_h)
        {
            Vector2 windowCursorPos = ImGuiNET.ImGui.GetCursorScreenPos();

            Vector2 region_max = new Vector2(0f, 0f);
            if (size.X < 0.0f || size.Y < 0.0f)
                region_max = GetContentRegionMaxAbs();

            if (size.X == 0.0f)
                size.X = default_w;
            else if (size.X < 0.0f)
                size.X = Math.Max(4.0f, region_max.X - windowCursorPos.X + size.X);

            if (size.Y == 0.0f)
                size.Y = default_h;
            else if (size.Y < 0.0f)
                size.Y = Math.Max(4.0f, region_max.Y - windowCursorPos.Y + size.Y);

            return size;
        }
        public static Vector2 GetContentRegionMaxAbs()
        {
            ImGui.GetCurrentContext();
            IntPtr ImGuiContext = ImGui.GetCurrentContext();
            Vector2 ImGuiWindowContentRegionRectMax = ImGui.GetWindowContentRegionMax();
            Vector2 mx = ImGuiWindowContentRegionRectMax;
            if (ImGui.GetColumnIndex() > 1)
                mx.X = ImGui.GetItemRectMax().X;
            return mx;
        }
        public static void TextHalfColored(string tempLabel, string tempLabelColored)
        {
            Vector2 meme = ImGui.GetCursorPos();
            ImGui.Text(tempLabel);
            ImGui.SetCursorPos(new Vector2(ImGui.CalcTextSize(tempLabel + " ").X, meme.Y));
            ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), tempLabelColored);
        }
        public static bool TreeNodePluso(string tempLabel, string tempLabelColored, Vector4 color, ImGuiTreeNodeFlags flags)
        {
            Vector2 meme = ImGui.GetCursorPos();
            string whitespace = "";
            foreach (char character in tempLabelColored)
            {
                whitespace += " ";
            }
            bool passThrough = ImGui.TreeNodeEx(tempLabel + whitespace, flags);
            ImGui.SetCursorPos(new Vector2(ImGui.CalcTextSize("   " + tempLabel + " ").X, meme.Y));
            ImGui.TextColored(color, tempLabelColored);
            return passThrough;
        }

        public static unsafe bool TreeNodePlusoOld(string label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        {

            Vector4 color = *ImGui.GetStyleColorVec4(ImGuiCol.ButtonHovered);
            bool firstNotColoredLabelFound = false;
            bool passThrough = false;
            string[] splitID = label.Split("##");
            foreach (string str in splitID[0].Split('%'))
            {
                if (str.Contains("'"))
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() - ImGui.CalcTextSize(" ").X);
                    ImGui.TextColored(color, str.Trim("'".ToCharArray()[0]));
                }
                else if (!firstNotColoredLabelFound)
                {
                    firstNotColoredLabelFound = true;
                    string id = "";
                    if (splitID.Length > 1)
                        id = splitID[1];
                    passThrough = ImGui.TreeNodeEx(str + "##" + id, flags);
                }
                else if (firstNotColoredLabelFound)
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() - ImGui.CalcTextSize(" ").X);
                    ImGui.Text(str);
                }
            }
            return passThrough;
        }
        public static unsafe bool TreeNodeTitleColored(string label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        {

            Vector4 color = *ImGui.GetStyleColorVec4(ImGuiCol.CheckMark);
            string[] splitID = label.Split("##");
            string[] textForLabelArray = splitID[0].Split("'");

            //SplitID and Text
            string id = "";
            if (splitID.Length > 1)
                id = splitID[1];

            //Create Padding String consisting of whitespaces
            string paddingString = "";
            for (int i = 1; i < textForLabelArray.Length; i++)
            {
                foreach (char chr in textForLabelArray[i])
                {
                    paddingString += " ";
                }
            }

            //Calculate Width of white space and width of padding
            float whitespaceCharWidth = ImGui.CalcTextSize(" ").X;
            float paddingWidth = whitespaceCharWidth * paddingString.Length;

            //Initialize TreeNode
            bool passThrough = ImGui.TreeNodeEx(textForLabelArray[0] + paddingString + "##" + id, flags);

            for (int i = 1; i < textForLabelArray.Length; i++)
            {
                if (i % 2 == 0)
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() - whitespaceCharWidth);
                    ImGui.Text(textForLabelArray[i]);
                }
                else
                {
                    ImGui.SameLine();
                    if (i == 1)
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - paddingWidth - whitespaceCharWidth);
                    else
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - whitespaceCharWidth);
                    ImGui.TextColored(color, textForLabelArray[i]);
                }
            }
            return passThrough;
        }
        public static bool TreeNodeTitleColored(string label, Vector4 textColor, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        {

            Vector4 color = textColor;
            string[] splitID = label.Split("##");
            string[] textForLabelArray = splitID[0].Split("'");

            //SplitID and Text
            string id = "";
            if (splitID.Length > 1)
                id = splitID[1];

            //Create Padding String consisting of whitespaces
            string paddingString = "";
            for (int i = 1; i < textForLabelArray.Length; i++)
            {
                foreach (char chr in textForLabelArray[i])
                {
                    paddingString += " ";
                }
            }

            //Calculate Width of white space and width of padding
            float whitespaceCharWidth = ImGui.CalcTextSize(" ").X;
            float paddingWidth = whitespaceCharWidth * paddingString.Length;

            //Initialize TreeNode
            bool passThrough = ImGui.TreeNodeEx(textForLabelArray[0] + paddingString + "##" + id, flags);

            for (int i = 1; i < textForLabelArray.Length; i++)
            {
                if (i % 2 == 0)
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() - whitespaceCharWidth);
                    ImGui.Text(textForLabelArray[i]);
                }
                else
                {
                    ImGui.SameLine();
                    if (i == 1)
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - paddingWidth - whitespaceCharWidth);
                    else
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - whitespaceCharWidth);
                    ImGui.TextColored(color, textForLabelArray[i]);
                }
            }
            return passThrough;
        }
    }
}
