using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DriveAnythingMod
{
    internal class InfoLabel : MonoBehaviour
    {
        float lastTime = Time.time;
        float prevSpeed = 0f;

        public bool labelEnabled = false;

        public string debugInfoString = "";

        public void Awake()
        {
        }

        public void OnGUI()
        {
            if (labelEnabled)
            {
                RenderLabel();
            }
        }

        private void RenderLabel()
        {
            Vector3 curCameraPosition = new Vector3(
                Camera.main.transform.position.x,
                Camera.main.transform.position.y,
                Camera.main.transform.position.z
            );

            float curTime = Time.time;
            float deltaTime = curTime - lastTime;
            float curSpeed = prevSpeed;

            RenderLabel(40, TextAnchor.UpperCenter, $"Position: (x: {Math.Floor(curCameraPosition.x)}, y: {Math.Floor(curCameraPosition.y)}, z: {Math.Floor(curCameraPosition.z)})", Color.white);

            RenderLabel(40, TextAnchor.LowerCenter, $"{debugInfoString}\n\n\n", Color.white);

            if (deltaTime > 0)
            {
                prevSpeed = curSpeed;
                lastTime = curTime;
            }
        }

        public void RenderLabel(int fontSize, TextAnchor alignment, string labelText, Color color, float offsetX = 0, float offsetY = 0)
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
                GUI.Label(new Rect(offsetX - x, offsetY, Screen.width, Screen.height), labelText, labelStyle);
                GUI.Label(new Rect(offsetX + x, offsetY, Screen.width, Screen.height), labelText, labelStyle);

                GUI.Label(new Rect(offsetX, offsetY - x, Screen.width, Screen.height), labelText, labelStyle);
                GUI.Label(new Rect(offsetX, offsetY + x, Screen.width, Screen.height), labelText, labelStyle);
            }

            labelStyle.normal.textColor = color;

            GUI.Label(new Rect(offsetX, offsetY, Screen.width, Screen.height), labelText, labelStyle);

            labelStyle.fontSize = oldFontSize;
            labelStyle.alignment = oldTextAnchor;
            labelStyle.fontStyle = oldFontStyle;

            labelStyle.normal.textColor = oldColor;
        }
    }
}
