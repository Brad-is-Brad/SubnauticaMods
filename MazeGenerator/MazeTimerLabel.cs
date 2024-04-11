using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MazeGeneratorMod
{
    internal class MazeTimerLabel : MonoBehaviour
    {
        bool timerEnabled = true;

        bool visible = false;
        bool running = false;

        float startTime = 0f;
        float endTime = 0f;

        public void ToggleTimerEnabled()
        {
            timerEnabled = !timerEnabled;

            if (timerEnabled)
            {
                ResetTimer();
            }
        }

        public bool IsTimerEnabled()
        {
            return timerEnabled;
        }

        public void Show()
        {
            visible = true;
        }

        public void Hide()
        {
            visible = false;
        }

        public void StartTimer()
        {
            if (!running)
            {
                running = true;
                startTime = Time.time;
            }
        }

        public void StopTimer()
        {
            if (running)
            {
                running = false;
                endTime = Time.time;
            }
        }

        public void ResetTimer()
        {
            running = false;
            startTime = 0f;
            endTime = 0f;
        }

        public void OnGUI()
        {
            if (visible && timerEnabled)
            {
                Render();
            }
        }

        private void Render()
        {
            float curTime;
            if (running)
            {
                curTime = Time.time - startTime;
            }
            else
            {
                curTime = endTime - startTime;
            }

            TimeSpan curTimeSpan = TimeSpan.FromSeconds(curTime);
            string timeString;
            if (curTimeSpan.TotalSeconds < 60)
            {
                timeString = curTimeSpan.ToString(@"s\.fff");
            }
            else if (curTimeSpan.TotalMinutes < 60)
            {
                timeString = curTimeSpan.ToString(@"m\:ss\.fff");
            }
            else
            {
                timeString = curTimeSpan.ToString(@"h\:mm\:ss\.fff");
            }

            string labelText = $"\n\nTime    \n{timeString}    ";

            RenderLabel(40, TextAnchor.UpperRight, labelText);
        }

        private void RenderLabel(int fontSize, TextAnchor alignment, string labelText)
        {
            GUIStyle labelStyle = GUI.skin.GetStyle("label");

            Color oldColor = labelStyle.normal.textColor;
            int oldFontSize = labelStyle.fontSize;
            TextAnchor oldTextAnchor = labelStyle.alignment;
            FontStyle oldFontStyle = labelStyle.fontStyle;

            labelStyle.fontSize = fontSize;
            labelStyle.alignment = alignment;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.normal.textColor = Color.black;

            int thickness = 1;

            for (int x = 1; x <= thickness; x++)
            {
                GUI.Label(new Rect(0 - x, 0, Screen.width, Screen.height), labelText, labelStyle);
                GUI.Label(new Rect(0 + x, 0, Screen.width, Screen.height), labelText, labelStyle);

                GUI.Label(new Rect(0, 0 - x, Screen.width, Screen.height), labelText, labelStyle);
                GUI.Label(new Rect(0, 0 + x, Screen.width, Screen.height), labelText, labelStyle);
            }

            labelStyle.normal.textColor = Color.white;

            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), labelText, labelStyle);

            labelStyle.fontSize = oldFontSize;
            labelStyle.alignment = oldTextAnchor;
            labelStyle.fontStyle = oldFontStyle;

            labelStyle.normal.textColor = oldColor;
        }
    }
}
