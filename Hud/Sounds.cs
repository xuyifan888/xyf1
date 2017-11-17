using System;
using System.Collections.Generic;
using System.Media;

namespace PoeHUD.Hud
{
    public static class Sounds
    {
        public static SoundPlayer AlertSound;
        public static SoundPlayer DangerSound;
        public static SoundPlayer TreasureSound;
        public static SoundPlayer AttentionSound;
        private static readonly Dictionary<string, SoundPlayer> soundLib = new Dictionary<string, SoundPlayer>();

        public static void AddSound(string name)
        {
            if (!soundLib.ContainsKey(name))
            {
                try
                {
                    var soundPlayer = new SoundPlayer($"sounds/{name}");
                    soundPlayer.Load();
                    soundLib[name] = soundPlayer;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error when loading {name}| {ex.Message}:", ex);
                }
            }
        }

        public static SoundPlayer GetSound(string name)
        {
            return soundLib[name];
        }

        public static void LoadSounds()
        {
            AddSound("alert.wav");
            AddSound("danger.wav");
            AddSound("treasure.wav");
            AddSound("attention.wav");
            AlertSound = GetSound("alert.wav");
            DangerSound = GetSound("danger.wav");
            TreasureSound = GetSound("treasure.wav");
            AttentionSound = GetSound("attention.wav");
        }
    }
}