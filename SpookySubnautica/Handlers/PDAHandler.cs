using FMOD;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SpookySubnautica.Handlers
{
    internal class PDAHandler
    {
        static Dictionary<string, Sound> sounds = new Dictionary<string, Sound>();
        public static string pdaBus = "bus:/master/SFX_for_pause/PDA_pause/all/all voice/AI voice";
        static float lastMessageTime = Time.time;
        static float timeBetweenEffects = 60 * 3;
        static float pdaVolume = 0.7f;

        static Sound dawnApproaching;
        static Sound duskApproaching;

        public static void Start()
        {
            LoadSounds();
        }

        static void LoadSounds()
        {
            string pdaSoundsPath = Path.Combine(Mod.ModPath, $"sounds\\pda\\");
            string[] filepaths = Directory.GetFiles(pdaSoundsPath);

            foreach (string filepath in filepaths)
            {
                string filename = Path.GetFileName(filepath);
                Sound sound = Mod.LoadSound(Path.Combine("pda\\", filename), MODE.DEFAULT, pdaBus);
                sounds.Add(filename, sound);
            }

            dawnApproaching = Mod.LoadSound("dawn approaching.ogg", MODE.DEFAULT, pdaBus);
            duskApproaching = Mod.LoadSound("dusk approaching.ogg", MODE.DEFAULT, pdaBus);
        }

        public static void Update()
        {
            if (
                Time.time - lastMessageTime > timeBetweenEffects
                && PDASounds.queue.current == null
                && Mod.CanPlayEffect(Mod.EffectType.PDA, true)
            )
            {
                Mod.UpdateLatestEffect(Mod.EffectType.PDA);
                PlayRandomPDAMessage();
                lastMessageTime = Time.time;
            }
        }

        public static void PlayDawnApproaching()
        {
            PlayPDASound(dawnApproaching);
        }

        public static void PlayDuskApproaching()
        {
            PlayPDASound(duskApproaching);
        }

        public static void PlayRandomPDAMessage()
        {
            int index = Mod.random.Next(sounds.Values.Count);
            PlayPDASound(sounds.Values.ToArray()[index]);
        }

        public static void PlayPDASound(Sound sound)
        {
            Mod.PlaySound(
                sound,
                pdaBus,
                out Channel channel
            );

            channel.setVolume(pdaVolume);
        }
    }
}
