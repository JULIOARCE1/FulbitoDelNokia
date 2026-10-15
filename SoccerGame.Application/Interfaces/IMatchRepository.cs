using System;
using System.Collections.Generic;
using System.Text;

using SoccerGame.Application.DTOs;

namespace SoccerGame.Application.Interfaces;

public interface IMatchRepository
{
    Task SaveMatchHistoryAsync(MatchStatusDto matchStatus);
}