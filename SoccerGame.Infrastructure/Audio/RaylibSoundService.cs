using System;
using System.Collections.Generic;
using System.Text;

using Raylib_cs;
using SoccerGame.Application.Interfaces;

namespace SoccerGame.Infrastructure.Audio;

public class RaylibSoundService : ISoundService
{
    public RaylibSoundService()
    {
        Raylib.InitAudioDevice();
    }

    public void PlayKickSound()
    {
        Task.Run(() =>
        {
            try { Console.Beep(220, 60); } catch { }
        });
    }

    public void PlayGoalWhistleSound()
    {
        Task.Run(() =>
        {
            try
            {
                Console.Beep(880, 120);
                Thread.Sleep(40);
                Console.Beep(880, 200);
            }
            catch { }
        });
    }

    public void PlayStartWhistleSound()
    {
        Task.Run(() =>
        {
            try { Console.Beep(750, 150); } catch { }
        });
    }
}