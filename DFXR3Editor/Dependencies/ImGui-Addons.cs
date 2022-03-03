using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using ImGuiNET;

namespace ImGuiNETAddons
{
    public static class ImGuiAddons
    {
        public static bool ToggleButton(string strId, ref bool v)
        {
            Vector2 p = ImGuiNET.ImGui.GetCursorScreenPos();
            ImDrawListPtr drawList = ImGuiNET.ImGui.GetWindowDrawList();
            bool isClicked = false;

            float height = ImGuiNET.ImGui.GetFrameHeight();
            float width = height * 1.55f;
            float radius = height * 0.50f;

            if (ImGuiNET.ImGui.InvisibleButton(strId, new Vector2(width, height)))
            {
                isClicked = true;
                v = !v;
            }
            uint colBg;
            uint colNub;
            if (ImGuiNET.ImGui.IsItemHovered())
            {
                colBg = v ? ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonHovered) : ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonActive, 0.5f);
                colNub = v ? ImGuiNET.ImGui.GetColorU32(ImGuiCol.ScrollbarGrabActive) : ImGuiNET.ImGui.GetColorU32(ImGuiCol.ScrollbarGrabActive);
            }
            else
            {
                colBg = v ? ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonHovered, 0.8f) : ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonHovered, 0.5f);
                colNub = v ? ImGuiNET.ImGui.GetColorU32(ImGuiCol.ScrollbarGrabHovered) : ImGuiNET.ImGui.GetColorU32(ImGuiCol.ScrollbarGrabHovered);
            }
            drawList.AddRectFilled(p, new Vector2(p.X + width, p.Y + height), colBg, height * 0.5f);
            drawList.AddCircleFilled(new Vector2(v ? (p.X + width - radius) : (p.X + radius), p.Y + radius), radius - 1.5f, colNub);
            return isClicked;
        }

        public static bool ButtonGradient(string strId)
        {
            String[] strIdArray = strId.Split("##");
            Vector2 p = ImGuiNET.ImGui.GetCursorScreenPos();
            Vector2 sizeText = ImGui.CalcTextSize(strIdArray[0]);
            ImDrawListPtr drawList = ImGuiNET.ImGui.GetWindowDrawList();
            float buttonHeight = ImGuiNET.ImGui.GetFrameHeight();
            float buttonWidth = sizeText.X + sizeText.X * 0.20f;
            Vector2 buttonSize = new Vector2(p.X + buttonWidth, p.Y + buttonHeight);
            uint colTop;
            uint colBottom;
            if (strIdArray.Length > 1)
            {
                if (ImGuiNET.ImGui.InvisibleButton(strIdArray[1], new Vector2(buttonWidth, buttonHeight)))
                {
                    return true;
                }
            }
            else
            {
                if (ImGuiNET.ImGui.InvisibleButton(strIdArray[0], new Vector2(buttonWidth, buttonHeight)))
                {
                    return true;
                }
            }
            if (ImGuiNET.ImGui.IsItemHovered())
            {
                colTop = ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonHovered, 1.50f);
                colBottom = ImGuiNET.ImGui.GetColorU32(ImGuiCol.Button, 0.50f);
            }
            else
            {
                colTop = ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonHovered);
                colBottom = ImGuiNET.ImGui.GetColorU32(ImGuiCol.Button, 0.20f);
            }
            drawList.AddRectFilledMultiColor(p, buttonSize, colTop, colTop, colBottom, colBottom);
            drawList.AddRect(p, buttonSize, ImGuiNET.ImGui.GetColorU32(ImGuiCol.Separator));
            drawList.AddText(new Vector2(p.X + (buttonWidth / 2) - (sizeText.X / 2), p.Y + (buttonHeight / 2) - (sizeText.Y / 2)), ImGui.GetColorU32(ImGuiCol.Text), strIdArray[0]);
            return false;
        }
        public static bool ButtonGradient(string strId, Vector2 buttonSizeVector)
        {
            String[] strIdArray = strId.Split("###");
            Vector2 p = ImGuiNET.ImGui.GetCursorScreenPos();
            Vector2 sizeText = ImGui.CalcTextSize(strIdArray[0]);
            ImDrawListPtr drawList = ImGuiNET.ImGui.GetWindowDrawList();
            //ButtonHeight += ImGuiNET.ImGui.GetFrameHeight(); //Dynamically Allocated Height
            //float ButtonWidth = sizeText.X + ButtonHeight; //Dynamically Allocated Width
            float buttonHeight = buttonSizeVector.Y; //Fixed Height
            float buttonWidth = buttonSizeVector.X; //Dynamically Allocated Width
            if (buttonWidth < sizeText.X)
            {
                buttonWidth = sizeText.X + sizeText.X * 0.20f;
            }
            Vector2 buttonSize = new Vector2(p.X + buttonWidth, p.Y + buttonHeight);
            uint colTop;
            uint colBottom;
            if (strIdArray.Length > 1)
            {
                if (ImGuiNET.ImGui.InvisibleButton(strIdArray[1], new Vector2(buttonWidth, buttonHeight)))
                {
                    return true;
                }
            }
            else
            {
                if (ImGuiNET.ImGui.InvisibleButton(strIdArray[0], new Vector2(buttonWidth, buttonHeight)))
                {
                    return true;
                }
            }
            if (ImGuiNET.ImGui.IsItemHovered())
            {
                colTop = ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonActive);
                colBottom = ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonHovered);
            }
            else
            {
                colTop = ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonHovered);
                colBottom = ImGuiNET.ImGui.GetColorU32(ImGuiCol.Button);
            }
            drawList.AddRectFilledMultiColor(p, buttonSize, colTop, colTop, colBottom, colBottom);
            drawList.AddRect(p, buttonSize, ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonActive));
            drawList.AddText(new Vector2(p.X + (buttonWidth / 2) - (sizeText.X / 2), p.Y + (buttonHeight / 2) - (sizeText.Y / 2)), ImGui.GetColorU32(ImGuiCol.Text), strIdArray[0]);
            return false;
        }
        public static Vector2 CalcItemSize(Vector2 size, float defaultW, float defaultH)
        {
            Vector2 windowCursorPos = ImGuiNET.ImGui.GetCursorScreenPos();

            Vector2 regionMax = new Vector2(0f, 0f);
            if (size.X < 0.0f || size.Y < 0.0f)
                regionMax = GetContentRegionMaxAbs();

            if (size.X == 0.0f)
                size.X = defaultW;
            else if (size.X < 0.0f)
                size.X = Math.Max(4.0f, regionMax.X - windowCursorPos.X + size.X);

            if (size.Y == 0.0f)
                size.Y = defaultH;
            else if (size.Y < 0.0f)
                size.Y = Math.Max(4.0f, regionMax.Y - windowCursorPos.Y + size.Y);

            return size;
        }
        public static Vector2 GetContentRegionMaxAbs()
        {
            ImGui.GetCurrentContext();
            IntPtr imGuiContext = ImGui.GetCurrentContext();
            Vector2 imGuiWindowContentRegionRectMax = ImGui.GetWindowContentRegionMax();
            Vector2 mx = imGuiWindowContentRegionRectMax;
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
        public static unsafe Vector4 GetStyleColorVec4Safe(ImGuiCol colorEnum) 
        { 
            return *ImGui.GetStyleColorVec4(colorEnum);
        }
        public static bool TreeNodeTitleColored(string label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        {
            Vector4 color = GetStyleColorVec4Safe(ImGuiCol.CheckMark);
            string[] splitId = label.Split("##");
            string[] textForLabelArray = splitId[0].Split("'");

            //SplitID and Text
            string id = "";
            if (splitId.Length > 1)
                id = splitId[1];

            //Create Padding String consisting of whitespaces
            string paddingString = "";
            for (int i = 0; i < textForLabelArray.Length; i++)
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
            bool passThrough = ImGui.TreeNodeEx(paddingString + "##" + id, flags);

            for (int i = 0; i < textForLabelArray.Length; i++)
            {
                if (i % 2 == 1)
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() - whitespaceCharWidth);
                    ImGui.TextColored(color, textForLabelArray[i]);
                }
                else
                {
                    ImGui.SameLine();
                    if (i == 0)
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - paddingWidth - whitespaceCharWidth);
                    else
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - whitespaceCharWidth);
                    ImGui.Text(textForLabelArray[i]);
                }
            }
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() - paddingWidth - (whitespaceCharWidth * 5 - 6f));
            ImGui.Text(paddingString + "     ");
            return passThrough;
        }
        public static bool TreeNodeTitleColored(string label, Vector4 highlightColor, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        {

            Vector4 color = highlightColor;
            string[] splitId = label.Split("##");
            string[] textForLabelArray = splitId[0].Split("'");

            //SplitID and Text
            string id = "";
            if (splitId.Length > 1)
                id = splitId[1];

            //Create Padding String consisting of whitespaces
            string paddingString = "";
            for (int i = 0; i < textForLabelArray.Length; i++)
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
            bool passThrough = ImGui.TreeNodeEx(paddingString + "##" + id, flags);

            for (int i = 0; i < textForLabelArray.Length; i++)
            {
                if (i % 2 == 1)
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() - whitespaceCharWidth);
                    ImGui.TextColored(color, textForLabelArray[i]);
                }
                else
                {
                    ImGui.SameLine();
                    if (i == 0)
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - paddingWidth - whitespaceCharWidth);
                    else
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - whitespaceCharWidth);
                    ImGui.Text(textForLabelArray[i]);
                }
            }
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() - paddingWidth - (whitespaceCharWidth * 5 - 6f));
            ImGui.Text(paddingString + "     ");
            return passThrough;
        }
        public static bool TreeNodeTitleColored(string label, Vector4 lowlightColor, Vector4 highlightColor, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        {
            string[] splitId = label.Split("##");
            string[] textForLabelArray = splitId[0].Split("'");

            //SplitID and Text
            string id = "";
            if (splitId.Length > 1)
                id = splitId[1];

            //Create Padding String consisting of whitespaces
            string paddingString = "";
            for (int i = 0; i < textForLabelArray.Length; i++)
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
            bool passThrough = ImGui.TreeNodeEx(paddingString + "##" + id, flags);

            for (int i = 0; i < textForLabelArray.Length; i++)
            {
                if (i % 2 == 1)
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() - whitespaceCharWidth);
                    ImGui.TextColored(highlightColor, textForLabelArray[i]);
                }
                else
                {
                    ImGui.SameLine();
                    if (i == 0)
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - paddingWidth - whitespaceCharWidth);
                    else
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - whitespaceCharWidth);
                    ImGui.TextColored(lowlightColor, textForLabelArray[i]);
                }
            }
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() - paddingWidth - (whitespaceCharWidth * 5 - 6f));
            ImGui.Text(paddingString + "     ");
            return passThrough;
        }
        public static bool IsItemHoveredForTime(float timeMs, float frameRate, string hoveredStringId)
        {
            bool hovered = ImGui.IsItemHovered();
            uint idStorage = ImGui.GetID(hoveredStringId);
            ImGuiStoragePtr storage = ImGui.GetStateStorage();
            int frameCounter = storage.GetInt(idStorage);

            int framesHoveredGoal = Convert.ToInt32((frameRate / 1000f) * timeMs);

            if (hovered & frameCounter < framesHoveredGoal)
                storage.SetInt(idStorage, frameCounter + 1);
            else if (!hovered & frameCounter > 0)
                storage.SetInt(idStorage, 0);

            if (frameCounter >= framesHoveredGoal)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool BeginComboFixed(string label, string previewValue )
        {
            string labelOnly = label.Contains("##") ? label.Split("##")[0] : label;
            ImGui.Text(labelOnly);
            ImGui.SameLine();
            string popupId = label + "Popup";
            var popupPos = ImGui.GetMousePosOnOpeningCurrentPopup();
            ImGui.ArrowButton("##" + label + "ButtonArrow", ImGuiDir.Right);
            ImGui.OpenPopupOnItemClick(popupId, ImGuiPopupFlags.MouseButtonLeft);
            ImGui.SameLine();
            var cursorPosAfterArrowButton = ImGui.GetCursorPos();
            ImGui.SetCursorPos(new Vector2(cursorPosAfterArrowButton.X - 10, cursorPosAfterArrowButton.Y));
            ButtonGradient(previewValue + "##" + label + "ButtonMain");
            ImGui.OpenPopupOnItemClick(popupId, ImGuiPopupFlags.MouseButtonLeft);
            if (ImGui.IsPopupOpen(popupId))
            {
                ImGui.SetNextWindowPos(popupPos, ImGuiCond.Appearing);
                ImGui.SetNextWindowSize(new Vector2(300, 400));
                return ImGui.BeginPopupContextItem(popupId);
            }
            return false;


        }
        public static void EndComboFixed()
        {
            ImGui.EndPopup();
        }
    }
}
