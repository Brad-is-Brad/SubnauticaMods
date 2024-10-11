using FMOD;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SpookySubnautica.Handlers
{
    internal class MetalThumpHandler
    {
        static float timeBetweenEffects = 60 * 2;
        static float lastEffectTime;
        static bool effectActive = false;

        static bool soundLoaded = false;
        static Sound sound;
        static string soundBus = "bus:/master/SFX_for_pause/PDA_pause/all/SFX/creatures surface";
        static Channel channel;
        static float soundVolume = 1f;

        public static void Update()
        {
            if (effectActive && (Player.main == null || Player.main.currentSub == null))
            {
                StopEffect();
                return;
            }

            if (Player.main == null) { return; };
            if (Player.main.currentSub == null) { return; };

            Base curBase = Player.main.currentSub.GetComponent<Base>();

            if (
                !effectActive
                && Time.time - lastEffectTime >= timeBetweenEffects
                && curBase != null
            )
            {
                StartEffect();
            }
            else if (effectActive && curBase == null)
            {
                StopEffect();
            }
        }

        public static void StartEffect()
        {
            effectActive = true;
            lastEffectTime = Time.time;

            if (!soundLoaded)
            {
                soundLoaded = true;
                sound = Mod.LoadSound("metal thump.ogg", MODE.DEFAULT, soundBus);
            }

            Mod.PlaySound(sound, soundBus, out channel);
            channel.setVolume(soundVolume);

            ATTRIBUTES_3D attributes = FMODUnity.RuntimeUtils.To3DAttributes(Camera.main.transform.position);
            channel.set3DAttributes(ref attributes.position, ref attributes.velocity);
        }

        public static void StopEffect()
        {
            effectActive = false;
            channel.stop();
        }
    }
}
