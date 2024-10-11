using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SpookySubnautica
{
    internal class InfoLabel : MonoBehaviour
    {
        public string debugInfoString = "";

        public void Awake()
        {
        }

        public void OnGUI()
        {
            RenderLabel(40, TextAnchor.LowerCenter, $"{debugInfoString}\n\n\n", Color.white);
        }

        public void RenderLabel(int fontSize, TextAnchor alignment, string labelText, Color color)
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

            labelStyle.normal.textColor = color;

            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), labelText, labelStyle);

            labelStyle.fontSize = oldFontSize;
            labelStyle.alignment = oldTextAnchor;
            labelStyle.fontStyle = oldFontStyle;

            labelStyle.normal.textColor = oldColor;
        }
    }
}
