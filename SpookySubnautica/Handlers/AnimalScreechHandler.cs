using FMOD;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SpookySubnautica.Handlers
{
    internal class AnimalScreechHandler
    {
        static float timeBetweenEffects = 60 * 5;
        static float lastEffectTime;

        static bool soundLoaded = false;
        static Sound sound;
        static string soundBus = "bus:/master/SFX_for_pause/PDA_pause/all/SFX/creatures surface";
        static Channel channel;
        static float soundVolume = 0.8f;

        public static void Update()
        {
            if (Player.main == null) { return; };
            if (Player.main.currentSub != null) { return; };

            if (
                Time.time - lastEffectTime >= timeBetweenEffects
            )
            {
                StartEffect();
            }
        }

        public static void StartEffect()
        {
            lastEffectTime = Time.time;

            if (!soundLoaded)
            {
                soundLoaded = true;
                sound = Mod.LoadSound("red fox screeching.ogg", MODE.DEFAULT, soundBus);
            }

            Mod.PlaySound(sound, soundBus, out channel);
            channel.setVolume(soundVolume);

            ATTRIBUTES_3D attributes = FMODUnity.RuntimeUtils.To3DAttributes(
                Camera.main.transform.position + (Camera.main.transform.forward * -5f)
            );
            channel.set3DAttributes(ref attributes.position, ref attributes.velocity);
        }
    }
}
