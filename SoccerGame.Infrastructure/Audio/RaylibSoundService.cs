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
        // Tono grave y muy corto para la patada
        Task.Run(() => Console.Beep(220, 80));
    }

    public void PlayGoalWhistleSound()
    {
        // Tono agudo tipo silbato de gol
        Task.Run(() =>
        {
            Console.Beep(880, 150);
            Thread.Sleep(50);
            Console.Beep(880, 250);
        });
    }

    public void PlayStartWhistleSound()
    {
        // Silbato corto de inicio
        Task.Run(() => Console.Beep(750, 200));
    }
}