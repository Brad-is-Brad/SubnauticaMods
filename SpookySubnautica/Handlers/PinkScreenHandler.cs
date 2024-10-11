using FMOD;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SpookySubnautica.Handlers
{
    internal class PinkScreenHandler
    {
        static LineRenderer lineRenderer;
        static Light light;

        static float timeBetweenEffects = 60 * 10;
        static float effectDuration = 3;

        static float lastEffectTime;
        static bool effectActive = false;

        static bool soundLoaded = false;
        static Sound sound;
        static string soundBus = "bus:/master/SFX_for_pause/PDA_pause/all/SFX/creatures surface";
        static Channel channel;
        static float soundVolume = 0.7f;

        static Vector3 forwardPosition;

        public static void Update()
        {
            if (Player.main == null || Camera.main == null) { lastEffectTime = Time.time + 10f; return; };

            if (
                !effectActive
                && Time.time - lastEffectTime >= timeBetweenEffects
                && Mod.CanPlayEffect(Mod.EffectType.PinkScreen, false)
            )
            {
                Mod.UpdateLatestEffect(Mod.EffectType.PinkScreen);
                StartEffect();
            }
            else if (effectActive && Time.time - lastEffectTime >= effectDuration)
            {
                StopEffect();
            }
            else if (effectActive)
            {
                Transform cameraTransform = Camera.main.transform;

                forwardPosition = cameraTransform.position + cameraTransform.forward * 1f;
                lineRenderer.SetPositions(new Vector3[] {
                    forwardPosition + cameraTransform.right * -3f,
                    forwardPosition + cameraTransform.right * 3f,
                });

                light.gameObject.transform.position = cameraTransform.position + -cameraTransform.forward * 1f;
            }
        }

        public static void StartEffect()
        {
            effectActive = true;
            lastEffectTime = Time.time;

            if (lineRenderer == null)
            {
                lineRenderer = Player.main.gameObject.AddComponent<LineRenderer>();
                lineRenderer.startWidth = 6f;
                lineRenderer.endWidth = 6f;
            }

            lineRenderer.enabled = true;

            if (light == null)
            {
                light = new GameObject().AddComponent<Light>();
                light.type = LightType.Point;
                light.color = Color.red;
                light.intensity = 6f;
            }

            light.enabled = true;

            if (!soundLoaded)
            {
                soundLoaded = true;
                sound = Mod.LoadSound("horror pink line sound.ogg", MODE.DEFAULT, soundBus);
            }

            Mod.PlaySound(sound, soundBus, out channel);
            channel.setVolume(soundVolume);
        }

        public static void StopEffect()
        {
            if (!effectActive) { return; }

            effectActive = false;
            lineRenderer.enabled = false;
            light.enabled = false;
            channel.stop();
        }
    }
}
