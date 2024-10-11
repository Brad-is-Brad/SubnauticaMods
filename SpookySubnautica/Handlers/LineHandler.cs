using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine;

namespace SpookySubnautica.Handlers
{
    internal class LineHandler
    {
        public static Material lineMaterial = null;

        public static IEnumerator Setup()
        {
            Plugin.Logger.LogInfo($"LineHandler.Setup()");
            AsyncOperationHandle<Material> resourceRequest2 = AddressablesUtility.LoadAsync<Material>("Materials/ghostmodel.mat");
            yield return resourceRequest2;
            resourceRequest2.LogExceptionIfFailed("Materials/ghostmodel.mat");
            lineMaterial = resourceRequest2.Result;
        }

        public static void DrawBox(LineRenderer lineRenderer, Vector3 position, Color color, float boxSize)
        {
            DrawLine(
                lineRenderer,
                color,
                new Vector3[] {
                    // bottom square
                    position + new Vector3(-boxSize, -boxSize, -boxSize),
                    position + new Vector3(-boxSize, -boxSize, boxSize),
                    position + new Vector3(boxSize, -boxSize, boxSize),
                    position + new Vector3(boxSize, -boxSize, -boxSize),
                    position + new Vector3(-boxSize, -boxSize, -boxSize),

                    // go up
                    position + new Vector3(-boxSize, boxSize, -boxSize),

                    // back left corner
                    position + new Vector3(-boxSize, boxSize, boxSize),
                    position + new Vector3(-boxSize, -boxSize, boxSize),
                    position + new Vector3(-boxSize, boxSize, boxSize),

                    // back right corner
                    position + new Vector3(boxSize, boxSize, boxSize),
                    position + new Vector3(boxSize, -boxSize, boxSize),
                    position + new Vector3(boxSize, boxSize, boxSize),

                    // front right corner
                    position + new Vector3(boxSize, boxSize, -boxSize),
                    position + new Vector3(boxSize, -boxSize, -boxSize),
                    position + new Vector3(boxSize, boxSize, -boxSize),

                    // back to front left top corner
                    position + new Vector3(-boxSize, boxSize, -boxSize),
                }
            );
        }

        public static void DrawLine(LineRenderer lineRenderer, Color color, Vector3[] positions)
        {
            try
            {
                if (lineRenderer == null) return;

                lineRenderer.startWidth = 0.1f;
                lineRenderer.endWidth = 0.1f;
                lineRenderer.sharedMaterial = lineMaterial;
                lineRenderer.positionCount = positions.Length;

                lineRenderer.startColor = color;
                lineRenderer.endColor = color;

                lineRenderer.SetPositions(positions);
            }
            catch (Exception e)
            {
                Plugin.Logger.LogInfo(e);
            }
        }
    }
}
