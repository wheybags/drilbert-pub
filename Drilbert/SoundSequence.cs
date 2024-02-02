using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace Drilbert;

public class SoundSequence
{
    List<SoundInstance> sounds;
    int position = 0;
    public bool playing {get; private set;}

    public SoundSequence(List<SoundInstance> sounds)
    {
       this.sounds = sounds;
    }

    public void play()
    {
        position = 0;
        sounds[position].State = SoundState.Playing;
        playing = true;
    }

    public void stop()
    {
        sounds[position].State = SoundState.Stopped;
        position = sounds.Count - 1;
        playing = false;
    }

    public SoundInstance currentSound()
    {
        return sounds[position];
    }

    public void update()
    {
        if (!playing)
            return;

        if (position < sounds.Count - 1 && sounds[position].State == SoundState.Stopped)
        {
            position++;
            sounds[position].State = SoundState.Playing;
        }
        else if (position == sounds.Count - 1 && sounds[position].State == SoundState.Stopped)
        {
            playing = false;
        }
    }
}