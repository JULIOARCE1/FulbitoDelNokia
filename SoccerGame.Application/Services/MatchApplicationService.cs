using System;
using System.Collections.Generic;
using System.Text;

using SoccerGame.Application.Commands;
using SoccerGame.Application.DTOs;
using SoccerGame.Application.Interfaces;
using SoccerGame.Domain.Entities;
using SoccerGame.Domain.ValueObjects;

namespace SoccerGame.Application.Services;

public class MatchApplicationService
{
    private readonly Dictionary<Guid, Match> _activeMatches = new();
    private readonly ISoundService? _soundService;
    private readonly IMatchRepository? _matchRepository;

    private Guid? _possessorPlayerId = null;
    private Guid? _stolenFromPlayerId = null; // Jugador a quien le quitaron la pelota (Inmunidad)
    private float _reStealImmunityTimer = 0f;
    private float _passCooldown = 0f;

    private readonly Dictionary<Guid, Position> _playerFacingDir = new();
    private readonly Random _random = new();

    public MatchApplicationService(ISoundService? soundService = null, IMatchRepository? matchRepository = null)
    {
        _soundService = soundService;
        _matchRepository = matchRepository;
    }

    public Guid CreateMatch(string homeTeamName, string awayTeamName, float durationSeconds = 180f)
    {
        var home = new Team(Guid.NewGuid(), homeTeamName);
        var away = new Team(Guid.NewGuid(), awayTeamName);

        // Local (River - Ataca hacia +X a la derecha)
        home.AddPlayer(new Player(Guid.NewGuid(), "Arquero H", 1, new Position(-330f, 0f), PlayerStats.Default));
        home.AddPlayer(new Player(Guid.NewGuid(), "Defensa H", 2, new Position(-180f, 60f), PlayerStats.Default));
        home.AddPlayer(new Player(Guid.NewGuid(), "Medio H", 8, new Position(-90f, -50f), PlayerStats.Default));
        home.AddPlayer(new Player(Guid.NewGuid(), "Delantero H", 9, new Position(-20f, 0f), PlayerStats.Default));

        // Visitante (Boca - Ataca hacia -X a la izquierda)
        away.AddPlayer(new Player(Guid.NewGuid(), "Arquero A", 12, new Position(330f, 0f), PlayerStats.Default));
        away.AddPlayer(new Player(Guid.NewGuid(), "Defensa A", 3, new Position(180f, -60f), PlayerStats.Default));
        away.AddPlayer(new Player(Guid.NewGuid(), "Medio A", 10, new Position(90f, 50f), PlayerStats.Default));
        away.AddPlayer(new Player(Guid.NewGuid(), "Delantero A", 11, new Position(20f, 0f), PlayerStats.Default));

        var ball = new Ball(Position.Zero);
        var match = new Match(Guid.NewGuid(), home, away, ball, durationSeconds);

        foreach (var p in home.Players) _playerFacingDir[p.Id] = new Position(1f, 0f);
        foreach (var p in away.Players) _playerFacingDir[p.Id] = new Position(-1f, 0f);

        _activeMatches[match.Id] = match;
        return match.Id;
    }

    public void StartMatch(StartMatchCommand command)
    {
        var match = GetMatchOrThrow(command.MatchId);
        if (match.State == MatchState.NotStarted)
        {
            match.Start();
            _soundService?.PlayStartWhistleSound();
        }
    }

    public void UpdateTick(UpdateTickCommand command)
    {
        var match = GetMatchOrThrow(command.MatchId);
        var previousState = match.State;
        float dt = command.SanitizedDeltaTime;

        if (_passCooldown > 0f) _passCooldown -= dt;
        if (_reStealImmunityTimer > 0f) _reStealImmunityTimer -= dt;
        else _stolenFromPlayerId = null;

        if (match.State == MatchState.GoalScored)
        {
            _possessorPlayerId = null;
            match.ResumePlay();
        }

        match.UpdateTick(dt);

        if (match.State == MatchState.InPlay)
        {
            UpdateBallAndPossession(match);
            UpdateGoalkeepers(match);
            UpdateFieldAI(match, dt);
        }

        if (previousState == MatchState.InPlay && match.State == MatchState.GoalScored)
        {
            _possessorPlayerId = null;
            _soundService?.PlayGoalWhistleSound();
        }

        if (previousState == MatchState.InPlay && match.State == MatchState.Finished)
        {
            _soundService?.PlayStartWhistleSound();
            var status = GetMatchStatus(match.Id);
            if (status != null && _matchRepository != null)
            {
                _ = _matchRepository.SaveMatchHistoryAsync(status);
            }
        }
    }

    private void UpdateBallAndPossession(Match match)
    {
        var allPlayers = match.HomeTeam.Players.Concat(match.AwayTeam.Players).ToList();

        if (_possessorPlayerId.HasValue)
        {
            var possessor = allPlayers.FirstOrDefault(p => p.Id == _possessorPlayerId.Value);
            if (possessor != null)
            {
                if (!_playerFacingDir.TryGetValue(possessor.Id, out var facing))
                {
                    facing = possessor.Position.X >= 0 ? new Position(-1f, 0f) : new Position(1f, 0f);
                }

                // Pelota ubicada justo frente al jugador según la dirección a la que mira
                float ballX = possessor.Position.X + (facing.X * 14f);
                float ballY = possessor.Position.Y + (facing.Y * 14f);
                match.Ball.SetPosition(ClampToPitch(match, new Position(ballX, ballY)));

                // ROBO / TACKLE: Un rival cercano le quita el balón si no tiene inmunidad activa
                foreach (var opponent in allPlayers)
                {
                    if (SameTeam(match, possessor, opponent)) continue;
                    if (opponent.Id == _stolenFromPlayerId && _reStealImmunityTimer > 0f) continue;

                    float distToPossessor = opponent.Position.DistanceTo(possessor.Position);

                    if (distToPossessor < 16f)
                    {
                        _stolenFromPlayerId = possessor.Id;
                        _reStealImmunityTimer = 1.0f; // 1 segundo de inmunidad para evitar robos continuos
                        _possessorPlayerId = opponent.Id;
                        _passCooldown = 0.25f;
                        _soundService?.PlayKickSound();
                        break;
                    }
                }
            }
            else
            {
                _possessorPlayerId = null;
            }
        }
        else
        {
            // Pelota libre: captura el jugador más cercano que se aproxima
            float ballSpeed = MathF.Sqrt(match.Ball.Velocity.X * match.Ball.Velocity.X + match.Ball.Velocity.Y * match.Ball.Velocity.Y);
            if (ballSpeed < 35f && _passCooldown <= 0f)
            {
                foreach (var player in allPlayers)
                {
                    if (player.Id == _stolenFromPlayerId && _reStealImmunityTimer > 0f) continue;

                    if (player.Position.DistanceTo(match.Ball.Position) < 18f)
                    {
                        _possessorPlayerId = player.Id;
                        match.Ball.Stop();
                        _soundService?.PlayKickSound();
                        break;
                    }
                }
            }
        }
    }

    private void UpdateGoalkeepers(Match match)
    {
        float ballY = match.Ball.Position.Y;

        // Arquero Local (#1)
        var homeGk = match.HomeTeam.Players.FirstOrDefault(p => p.ShirtNumber == 1);
        if (homeGk != null)
        {
            float targetY = Math.Clamp(ballY, -55f, 55f);
            homeGk.MoveTo(new Position(-330f, targetY));

            // Si la pelota entra al área chica local, el arquero ataja y despeja
            if (match.Ball.Position.X < -310f && MathF.Abs(match.Ball.Position.Y) < 60f)
            {
                match.Ball.ApplyImpulse(65f, (_random.NextSingle() - 0.5f) * 30f);
                _soundService?.PlayKickSound();
                _possessorPlayerId = null;
            }
        }

        // Arquero Visitante (#12)
        var awayGk = match.AwayTeam.Players.FirstOrDefault(p => p.ShirtNumber == 12);
        if (awayGk != null)
        {
            float targetY = Math.Clamp(ballY, -55f, 55f);
            awayGk.MoveTo(new Position(330f, targetY));

            // Si la pelota entra al área chica visitante, despeja
            if (match.Ball.Position.X > 310f && MathF.Abs(match.Ball.Position.Y) < 60f)
            {
                match.Ball.ApplyImpulse(-65f, (_random.NextSingle() - 0.5f) * 30f);
                _soundService?.PlayKickSound();
                _possessorPlayerId = null;
            }
        }
    }

    private void UpdateFieldAI(Match match, float deltaTime)
    {
        var ballPos = match.Ball.Position;
        var awayPlayers = match.AwayTeam.Players.Where(p => p.ShirtNumber != 12).ToList();
        var homePlayers = match.HomeTeam.Players.Where(p => p.ShirtNumber != 1).ToList();

        var controlledPlayerId = GetClosestHomePlayerId(match);

        // --- IA RIVAL (VISITANTE) ---
        var closestEnemy = awayPlayers.OrderBy(p => p.Position.DistanceTo(ballPos)).FirstOrDefault();

        foreach (var enemy in awayPlayers)
        {
            if (_possessorPlayerId == enemy.Id)
            {
                // Conducción hacia el arco humano (X = -350)
                Position attackTarget = new Position(-350f, enemy.Position.Y * 0.4f);
                MovePlayerInternal(match, enemy, attackTarget, deltaTime, 1.0f);

                if (_passCooldown <= 0f)
                {
                    float distToGoal = enemy.Position.DistanceTo(new Position(-350f, 0f));

                    // REMATE AL ARCO
                    if (enemy.Position.X < -40f || distToGoal < 200f)
                    {
                        float targetY = (_random.Next(0, 2) == 0) ? -35f : 35f;
                        ExecuteKickTowards(match, enemy, new Position(-350f, targetY), 75f);
                        _passCooldown = 1.0f;
                    }
                    else
                    {
                        // PASE A UN COMPAÑERO LIBRE
                        var mate = awayPlayers.FirstOrDefault(p => p.Id != enemy.Id);
                        if (mate != null && _random.Next(0, 100) < 30)
                        {
                            ExecuteKickTowards(match, enemy, mate.Position, 50f);
                            _passCooldown = 0.8f;
                        }
                    }
                }
            }
            else if (enemy.Id == closestEnemy?.Id)
            {
                // Presión directa al balón
                MovePlayerInternal(match, enemy, ballPos, deltaTime, 1.05f);
            }
            else
            {
                // Posicionamiento en abanico táctico
                float targetY = (enemy.ShirtNumber % 2 == 0) ? 70f : -70f;
                float targetX = Math.Clamp(ballPos.X - 50f, -280f, 280f);
                MovePlayerInternal(match, enemy, new Position(targetX, targetY), deltaTime, 0.65f);
            }
        }

        // --- DESMARQUE DE COMPAÑEROS LOCALES (HUMANO) ---
        foreach (var teammate in homePlayers.Where(p => p.Id != controlledPlayerId))
        {
            float targetY = (teammate.ShirtNumber % 2 == 0) ? 80f : -80f;
            float targetX = Math.Clamp(ballPos.X + 60f, -280f, 280f);
            MovePlayerInternal(match, teammate, new Position(targetX, targetY), deltaTime, 0.65f);
        }
    }

    private void MovePlayerInternal(Match match, Player player, Position target, float deltaTime, float speedFactor)
    {
        float dx = target.X - player.Position.X;
        float dy = target.Y - player.Position.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        if (dist > 3f)
        {
            float dirX = dx / dist;
            float dirY = dy / dist;
            _playerFacingDir[player.Id] = new Position(dirX, dirY);

            float speed = player.Stats.Speed * 35f * speedFactor;
            float nx = player.Position.X + (dirX * speed * deltaTime);
            float ny = player.Position.Y + (dirY * speed * deltaTime);

            player.MoveTo(ClampToPitch(match, new Position(nx, ny)));
        }
    }

    private void ExecuteKickTowards(Match match, Player kicker, Position target, float power)
    {
        float dx = target.X - kicker.Position.X;
        float dy = target.Y - kicker.Position.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        if (dist > 0f)
        {
            _possessorPlayerId = null;
            _stolenFromPlayerId = kicker.Id;
            _reStealImmunityTimer = 0.4f;

            _playerFacingDir[kicker.Id] = new Position(dx / dist, dy / dist);
            match.Ball.ApplyImpulse((dx / dist) * power, (dy / dist) * power);
            _soundService?.PlayKickSound();
        }
    }

    public void MovePlayer(MovePlayerCommand command)
    {
        var match = GetMatchOrThrow(command.MatchId);
        if (match.State != MatchState.InPlay) return;

        var player = match.HomeTeam.Players.FirstOrDefault(p => p.Id == command.PlayerId)
                     ?? match.AwayTeam.Players.FirstOrDefault(p => p.Id == command.PlayerId);

        if (player is null) return;

        float inputX = command.SanitizedDeltaX;
        float inputY = command.SanitizedDeltaY;

        if (inputX != 0f || inputY != 0f)
        {
            float len = MathF.Sqrt(inputX * inputX + inputY * inputY);
            _playerFacingDir[player.Id] = new Position(inputX / len, inputY / len);

            float speed = player.Stats.Speed * 35f;
            float newX = player.Position.X + (inputX / len * speed * 0.016f);
            float newY = player.Position.Y + (inputY / len * speed * 0.016f);

            player.MoveTo(ClampToPitch(match, new Position(newX, newY)));
        }
    }

    public void KickBall(KickBallCommand command)
    {
        var match = GetMatchOrThrow(command.MatchId);
        if (match.State != MatchState.InPlay) return;

        var controlledPlayerId = GetClosestHomePlayerId(match);
        var player = match.HomeTeam.Players.FirstOrDefault(p => p.Id == controlledPlayerId);

        if (player != null && (_possessorPlayerId == player.Id || player.Position.DistanceTo(match.Ball.Position) < 22f))
        {
            float dirX = command.DirectionX;
            float dirY = command.DirectionY;

            // Si es pase corto (fuerza < 50), busca automáticamente al compañero más cercano
            if (command.Power < 50f)
            {
                var teammate = match.HomeTeam.Players
                    .Where(p => p.ShirtNumber != 1 && p.Id != player.Id)
                    .OrderBy(p => p.Position.DistanceTo(player.Position))
                    .FirstOrDefault();

                if (teammate != null)
                {
                    ExecuteKickTowards(match, player, teammate.Position, 45f);
                    _passCooldown = 0.35f;
                    return;
                }
            }

            if (dirX == 0f && dirY == 0f)
            {
                dirX = 1f;
                dirY = (_random.Next(0, 2) == 0) ? -0.2f : 0.2f;
            }

            Position target = new Position(player.Position.X + dirX * 100f, player.Position.Y + dirY * 100f);
            ExecuteKickTowards(match, player, target, command.Power);
            _passCooldown = 0.35f;
        }
    }

    private static bool SameTeam(Match match, Player? p1, Player? p2)
    {
        if (p1 == null || p2 == null) return false;
        bool p1Home = match.HomeTeam.Players.Any(p => p.Id == p1.Id);
        bool p2Home = match.HomeTeam.Players.Any(p => p.Id == p2.Id);
        return p1Home == p2Home;
    }

    public Guid GetClosestHomePlayerId(Match match)
    {
        return match.HomeTeam.Players
            .Where(p => p.ShirtNumber != 1)
            .OrderBy(p => p.Position.DistanceTo(match.Ball.Position))
            .First().Id;
    }

    private static Position ClampToPitch(Match match, Position position)
    {
        float halfWidth = match.PitchWidth / 2f - 12f;
        float halfHeight = match.PitchHeight / 2f - 12f;
        return new Position(Math.Clamp(position.X, -halfWidth, halfWidth), Math.Clamp(position.Y, -halfHeight, halfHeight));
    }

    public MatchStatusDto? GetMatchStatus(Guid matchId)
    {
        if (!_activeMatches.TryGetValue(matchId, out var match))
            return null;

        var playersDto = new List<PlayerDto>();

        foreach (var p in match.HomeTeam.Players)
            playersDto.Add(new PlayerDto(p.Id, p.Name, p.ShirtNumber, p.Position.X, p.Position.Y, IsHomeTeam: true));

        foreach (var p in match.AwayTeam.Players)
            playersDto.Add(new PlayerDto(p.Id, p.Name, p.ShirtNumber, p.Position.X, p.Position.Y, IsHomeTeam: false));

        return new MatchStatusDto(
            match.Id,
            match.HomeTeam.Name,
            match.AwayTeam.Name,
            match.Score.HomeGoals,
            match.Score.AwayGoals,
            match.State.ToString(),
            match.ElapsedTimeInSeconds,
            match.Ball.Position.X,
            match.Ball.Position.Y,
            playersDto.AsReadOnly()
        );
    }

    private Match GetMatchOrThrow(Guid matchId)
    {
        if (!_activeMatches.TryGetValue(matchId, out var match))
            throw new KeyNotFoundException($"No se encontró el partido con ID {matchId}");

        return match;
    }
}