using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace SpookySubnautica.Handlers
{
    internal class ColorHandler
    {
        static List<string> largeWorldEntityNames = new List<string>();
        static Color blankColor = new Color(0f, 0f, 0f, 0f);

        public static void Update()
        {
            /*
            // The "turn everything red" button
            if (Input.GetKeyDown(KeyCode.M))
            {
                try
                {
                    GetAllLookingAt();
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogInfo($"Error GetAllLookingAt: {e}");
                }
            }
            */
        }

        [HarmonyPatch(typeof(Base))]
        [HarmonyPatch("RebuildGeometry")]
        public class Patch_Base_RebuildGeometry : MonoBehaviour
        {
            [HarmonyPostfix]
            public static void Postfix(Base __instance)
            {
                RecolorBase(__instance.gameObject);
            }
        }

        static void RecolorBase(GameObject gameObject)
        {
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.materials)
                {
                    Color materialGlowColor = material.GetColor(ShaderPropertyID._GlowColor);
                    if (materialGlowColor != blankColor && materialGlowColor != Color.black)
                    {
                        material.SetColor(ShaderPropertyID._GlowColor, new Color(1f, 0f, 0f));
                    }

                    if (material.name.Contains("x_MapRoom_HoloTableGlow"))
                    {
                        material.SetColor(ShaderPropertyID._Color, Color.red);
                    }
                    else if (material.name.Contains("RoomMapBlip"))
                    {
                        foreach (FieldInfo fieldInfo in typeof(ShaderPropertyID).GetFields(BindingFlags.Public | BindingFlags.Static))
                        {
                            Color matColor = material.GetColor((int)fieldInfo.GetValue(null));
                            if (matColor != blankColor && matColor != Color.black)
                            {
                                Plugin.Logger.LogInfo($"HoloTable Color: {fieldInfo.Name} - {matColor}");
                                material.SetColor((int)fieldInfo.GetValue(null), Color.red);
                            }
                        }
                    }
                }
            }

            MiniWorld miniWorld = gameObject.GetComponentInChildren<MiniWorld>();
            if (miniWorld != null)
            {
                miniWorld.hologramMaterial.SetColor(ShaderPropertyID._Color, new Color(1f, 0.29f, 0.047f));
            }
        }

        [HarmonyPatch(typeof(LargeWorldEntity))]
        [HarmonyPatch("Start")]
        public class Patch_LargeWorldEntity_Start : MonoBehaviour
        {
            [HarmonyPostfix]
            public static void Postfix(LargeWorldEntity __instance)
            {
                try
                {
                    bool matched = false;
                    bool isNew = false;

                    if (!largeWorldEntityNames.Contains(__instance.name))
                    {
                        isNew = true;
                        largeWorldEntityNames.Add(__instance.name);
                        //Plugin.Logger.LogInfo($"New LargeWorldEntity: {__instance.name}");
                    }

                    foreach (string key in Plugin.config.configColors.Keys)
                    {
                        if (__instance.name.Contains(key))
                        {
                            matched = true;
                            Renderer[] renderers = __instance.gameObject.GetComponentsInChildren<Renderer>();
                            foreach (Renderer renderer in renderers)
                            {
                                foreach (Material material in renderer.materials)
                                {
                                    for (int i = 0; i < Plugin.config.configColors[key].Count; i++)
                                    {
                                        material.SetColor(
                                            (int)typeof(ShaderPropertyID)
                                                .GetField(Plugin.config.configColors[key].Keys.ToArray()[i])
                                                .GetValue(null)
                                            ,
                                            new Color(
                                                Plugin.config.configColors[key].Values.ToArray()[i][0],
                                                Plugin.config.configColors[key].Values.ToArray()[i][1],
                                                Plugin.config.configColors[key].Values.ToArray()[i][2],
                                                Plugin.config.configColors[key].Values.ToArray()[i][3]
                                            )
                                        );
                                    }
                                }
                            }

                            break; // Exit loop after first match
                        }
                    }

                    if (__instance.name.Equals("Base(Clone)"))
                    {
                        matched = true;
                        RecolorBase(__instance.gameObject);
                    }
                    else if (__instance.GetComponent<Creature>() != null && !matched)
                    {
                        matched = true;

                        Renderer[] renderers = __instance.gameObject.GetComponentsInChildren<Renderer>();
                        foreach (Renderer renderer in renderers)
                        {
                            foreach (Material material in renderer.materials)
                            {
                                material.SetColor(ShaderPropertyID._Color, new Color(1f, 0.2f, 0f));
                                material.SetColor(ShaderPropertyID._GlowColor, new Color(1f, 0f, 0f));
                            }
                        }
                    }

                    if (!matched && isNew)
                    {
                        //Plugin.Logger.LogInfo($"Unmatched: {__instance}");
                    }
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogInfo($"Error re-coloring - {e}");
                }
            }
        }

        public static void ExamineColors(GameObject gameObject)
        {
            Plugin.Logger.LogInfo($"ExamineColors - GameObject: {gameObject}");
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Plugin.Logger.LogInfo($"Renderer: {renderer}");
                foreach (Material material in renderer.materials)
                {
                    Plugin.Logger.LogInfo($"Material: {material}");
                    foreach (FieldInfo fieldInfo in typeof(ShaderPropertyID).GetFields(BindingFlags.Public | BindingFlags.Static))
                    {
                        Color matColor = material.GetColor((int)fieldInfo.GetValue(null));
                        if (matColor != blankColor && matColor != Color.black)
                        {
                            Plugin.Logger.LogInfo($"Color: {fieldInfo.Name} - {matColor}");
                        }
                    }
                }
            }
        }

        public static void GetAllLookingAt()
        {
            try
            {
                Vector3 position = Camera.main.transform.position;
                int num = UWE.Utils.SpherecastIntoSharedBuffer(
                    position,
                    1.0f, //spherecastRadius
                    Camera.main.transform.forward,
                    10f // maxDistance
                );

                for (int i = 0; i < num; i++)
                {
                    RaycastHit raycastHit = UWE.Utils.sharedHitBuffer[i];

                    //Mod.LogDebug($"SphereCast raycastHit - {raycastHit.collider.gameObject}");
                    GameObject rootGameObject = raycastHit.collider.gameObject;
                    Plugin.Logger.LogInfo($"raycastHit: {rootGameObject}");

                    if (
                        (
                            rootGameObject.name.Contains("Landscape")
                            || rootGameObject.name.Contains("SerializerEmptyGameObject")
                        )
                    )
                    {
                        continue;
                    }

                    for (int j = 0; j < 10; j++)
                    {
                        Plugin.Logger.LogInfo($"rootGameObject: {rootGameObject}");
                        if (j == 0)
                        {
                            Mod.PrintComponents(rootGameObject, "rootGameObject", true, true, true);
                        }

                        if (
                            rootGameObject == null
                            || rootGameObject.transform == null
                            || rootGameObject.transform.parent == null
                            || rootGameObject.transform.parent.gameObject == null
                            || rootGameObject == rootGameObject.transform.parent.gameObject
                        )
                            break;

                        if (rootGameObject.name.Contains("Base") && rootGameObject.transform.parent.gameObject.name.Contains("Base"))
                        {
                            //Mod.LogDebug($"Parent is also base: {rootGameObject} -  {rootGameObject.transform.parent.gameObject}");
                            break;
                        }

                        if (
                            (
                                rootGameObject.transform.parent.gameObject.name.Contains("Landscape")
                                || rootGameObject.transform.parent.gameObject.name.Contains("SerializerEmptyGameObject")
                            )
                        )
                        {
                            break;
                        }

                        rootGameObject = rootGameObject.transform.parent.gameObject;
                        TotallyRecolorObject(rootGameObject, Color.red);
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError($"{e}");
            }
        }

        public static void TotallyRecolorObject(GameObject gameObject, Color color)
        {
            Plugin.Logger.LogInfo($"Totally Re-coloring - {gameObject}");
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                Plugin.Logger.LogInfo($"Renderer - {renderer}");
                RendererMaterialsStorage rendererMaterialsStorage =
                    RendererMaterialsStorageManager.GetRendererMaterialsStorage(renderer, gameObject);

                bool success = true;
                Material material;
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        material = rendererMaterialsStorage.GetOrCreateCopiedMaterial(i);
                        if (material != null)
                        {
                            Plugin.Logger.LogInfo($"Material - {renderer}");

                            foreach (FieldInfo fieldInfo in typeof(ShaderPropertyID).GetFields(BindingFlags.Public | BindingFlags.Static))
                            {
                                Color matColor = material.GetColor((int)fieldInfo.GetValue(null));
                                if (matColor != blankColor && matColor != Color.black)
                                {
                                    material.SetColor((int)fieldInfo.GetValue(null), color);
                                    Plugin.Logger.LogInfo($"ShaderPropertyID - {fieldInfo.GetValue(null)}");
                                }
                            }
                        }
                        else
                        {
                            Plugin.Logger.LogInfo($"material {i} fail - {success} - {material}");
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        break;
                    }
                }
            }
        }
    }
}
