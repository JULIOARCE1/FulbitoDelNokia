using System;
using System.Collections.Generic;
using System.Text;

namespace SoccerGame.Application.Interfaces;

public interface ISoundService
{
    void PlayKickSound();
    void PlayGoalWhistleSound();
    void PlayStartWhistleSound();
}