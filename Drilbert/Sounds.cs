using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework.Audio;

namespace Drilbert
{
    public class Sound
    {
        private SoundEffect sound;
        public float volume { get; private set; }
        public Sound(string path, float volume = 1)
        {
            sound = SoundEffect.FromFile(Path.Combine(Constants.rootPath, path));
            this.volume = volume;
        }

        public SoundInstance createInstance()
        {
            return new SoundInstance(this, sound.CreateInstance());
        }

        public void Play()
        {
            createInstance().State = SoundState.Playing;
        }
    }

    public class SoundInstance
    {
        private Sound sound;
        private SoundEffectInstance instance;

        public SoundInstance(Sound sound, SoundEffectInstance instance)
        {
            this.sound = sound;
            this.instance = instance;
            Volume = Volume;
        }

        float _volume = 1;
        public float Volume
        {
            get
            {
                return  _volume;
            }

            set
            {
                _volume = value;
                instance.Volume = _volume * sound.volume;
            }
        }

        public float Pitch { get => instance.Pitch; set => instance.Pitch = value; }
        public bool IsLooped { get => instance.IsLooped; set => instance.IsLooped = value; }

        public SoundState State
        {
            get
            {
                return instance.State;
            }
            set
            {
                if (instance.State == value)
                    return;

                if (value == SoundState.Stopped)
                    instance.Stop();
                else if (value == SoundState.Paused)
                    instance.Pause();
                else if (value == SoundState.Playing)
                    instance.Play();
            }
        }
    }

    public static class Sounds
    {
        public static Dictionary<SoundId, Sound> soundEffects = new Dictionary<SoundId, Sound>();
        public static Sound menuSelect { get; private set; }
        public static Sound menuActivate { get; private set; }
        public static Sound menuClose { get; private set; }
        public static Sound levelSelect { get; private set; }
        public static List<Sound> musicTracks = new List<Sound>();
        public static Sound victoryBurst;
        public static Sound victoryLoop;

        public static void loadSoundEffects()
        {
            soundEffects[SoundId.Move] = new Sound("sfx/SFX_Jump_09.wav", decibelToLinear(-30));
            soundEffects[SoundId.Drill] = new Sound("sfx/drill.wav", decibelToLinear(-20));
            soundEffects[SoundId.Error] = new Sound("sfx/error_006.wav", decibelToLinear(-25));
            soundEffects[SoundId.Death] = new Sound("sfx/15_hit.wav", decibelToLinear(-15));
            soundEffects[SoundId.Coin] = new Sound("sfx/coin1.wav", decibelToLinear(-15));
            soundEffects[SoundId.BigMovement] = new Sound("sfx/explosionCrunch_000.wav", decibelToLinear(-20));
            soundEffects[SoundId.Megadrill] = new Sound("sfx/megadrill.wav", decibelToLinear(-20));
            soundEffects[SoundId.EquipmentPickup] = new Sound("sfx/equipment.wav", decibelToLinear(-30));
            soundEffects[SoundId.Diamond] = new Sound("sfx/diamond.wav", decibelToLinear(0));
            menuSelect = new Sound("sfx/click_002.wav", decibelToLinear(-20));
            menuActivate = new Sound("sfx/drop_003.wav", decibelToLinear(-20));
            menuClose = new Sound("sfx/switch_005.wav", decibelToLinear(-20));
            levelSelect = new Sound("sfx/question_001.wav", decibelToLinear(-20));

            musicTracks.Add(new Sound("sfx/Spy 2.wav", decibelToLinear(-20)));
            musicTracks.Add(new Sound("sfx/Song 1.wav", decibelToLinear(-20)));
            musicTracks.Add(new Sound("sfx/Song 2.wav", decibelToLinear(-20)));
            musicTracks.Add(new Sound("sfx/Song 3.wav", decibelToLinear(-20)));

            victoryBurst = new Sound("sfx/Victory Burst.wav", decibelToLinear(-15));
            victoryLoop = new Sound("sfx/Victory Loop.wav", decibelToLinear(-15));
        }

        public static float linearToDecibel(float linear)
        {
            return 20 * MathF.Log10(linear);
        }
        public static float decibelToLinear(float decibel)
        {
            return MathF.Pow(10, decibel / 20);
        }
    }
}