using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace Drilbert
{
    public static class MusicManager
    {
        private static SoundInstance musicInstance = null;
        private static int musicIndex = -1;
        private static long lastTrackEndTime = -1;
        private static SoundSequence victorySequence = null;
        private static long stopVictorySequenceTime = -1000*1000;
        private static long lastUpdateTime = -1;

        public static float dynamicVolume = 1;
        public static float dynamicPitch = 0;

        public static void resetDynamics()
        {
            dynamicVolume = 1;
            dynamicPitch = 0;
        }

        private static void applyDynamics(long gameTimeMs)
        {
            float alpha = MathF.Min(((float)(gameTimeMs - stopVictorySequenceTime)) / (3.0f * 1000), 1.0f);
            musicInstance.Volume = dynamicVolume * alpha * GameSettings.musicVolume;
            musicInstance.Pitch = dynamicPitch;
        }

        public static void playVictorySequence()
        {
            if (victorySequence == null)
            {
                SoundInstance loop = Sounds.victoryLoop.createInstance();
                loop.IsLooped = true;
                victorySequence = new SoundSequence(new List<SoundInstance>(new []{Sounds.victoryBurst.createInstance(), loop }));
            }

            if (musicInstance != null)
                musicInstance.State = SoundState.Paused;

            victorySequence.play();
        }

        public static void stopVictorySequence()
        {
            victorySequence.stop();
            stopVictorySequenceTime = lastUpdateTime;

            if (musicInstance != null)
                musicInstance.State = SoundState.Playing;
        }

        public static void nextTrack()
        {
            if (musicInstance != null)
                musicInstance.State = SoundState.Stopped;

            musicIndex = (musicIndex + 1) % Sounds.musicTracks.Count;
            musicInstance = Sounds.musicTracks[musicIndex].createInstance();
            applyDynamics(lastUpdateTime);
            musicInstance.State = SoundState.Playing;
        }

        public static void update(long gameTimeMs)
        {
            if (victorySequence == null || !victorySequence.playing)
            {
                if (musicInstance.State == SoundState.Stopped)
                {
                    if (lastTrackEndTime == -1)
                        lastTrackEndTime = gameTimeMs;

                    if (gameTimeMs - lastTrackEndTime > 1000 * 2)
                    {
                        nextTrack();
                        lastTrackEndTime = -1;
                    }
                }
                else
                {
                    musicInstance.State = Game1.game.IsActive ? SoundState.Playing : SoundState.Paused;
                }

                applyDynamics(gameTimeMs);
            }
            else
            {
                victorySequence.update();
            }

            lastUpdateTime = gameTimeMs;
        }
    }
}