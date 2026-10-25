using System.Numerics;
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

    private readonly Dictionary<Guid, Vector2> _playerFacingDir = new();
    private readonly Dictionary<Guid, float> _aiDecisionTimers = new();
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

        // Local (River Plate)
        home.AddPlayer(new Player(Guid.NewGuid(), "Arquero H", 1, new Position(-330f, 0f), PlayerStats.Default));
        home.AddPlayer(new Player(Guid.NewGuid(), "Defensa H", 2, new Position(-180f, 60f), PlayerStats.Default));
        home.AddPlayer(new Player(Guid.NewGuid(), "Medio H", 8, new Position(-90f, -50f), PlayerStats.Default));
        home.AddPlayer(new Player(Guid.NewGuid(), "Delantero H", 9, new Position(-20f, 0f), PlayerStats.Default));

        // Visitante (Boca Juniors)
        away.AddPlayer(new Player(Guid.NewGuid(), "Arquero A", 12, new Position(330f, 0f), PlayerStats.Default));
        away.AddPlayer(new Player(Guid.NewGuid(), "Defensa A", 3, new Position(180f, -60f), PlayerStats.Default));
        away.AddPlayer(new Player(Guid.NewGuid(), "Medio A", 10, new Position(90f, 50f), PlayerStats.Default));
        away.AddPlayer(new Player(Guid.NewGuid(), "Delantero A", 11, new Position(20f, 0f), PlayerStats.Default));

        var ball = new Ball(Vector2.Zero);
        var match = new Match(Guid.NewGuid(), home, away, ball, durationSeconds);

        ResetFormations(match);

        _activeMatches[match.Id] = match;
        return match.Id;
    }

    private void ResetFormations(Match match)
    {
        var homePlayers = match.HomeTeam.Players.ToList();
        var awayPlayers = match.AwayTeam.Players.ToList();

        homePlayers.FirstOrDefault(p => p.ShirtNumber == 1)?.MoveTo(new Position(-330f, 0f));
        homePlayers.FirstOrDefault(p => p.ShirtNumber == 2)?.MoveTo(new Position(-180f, 60f));
        homePlayers.FirstOrDefault(p => p.ShirtNumber == 8)?.MoveTo(new Position(-90f, -50f));
        homePlayers.FirstOrDefault(p => p.ShirtNumber == 9)?.MoveTo(new Position(-20f, 0f));

        awayPlayers.FirstOrDefault(p => p.ShirtNumber == 12)?.MoveTo(new Position(330f, 0f));
        awayPlayers.FirstOrDefault(p => p.ShirtNumber == 3)?.MoveTo(new Position(180f, -60f));
        awayPlayers.FirstOrDefault(p => p.ShirtNumber == 10)?.MoveTo(new Position(90f, 50f));
        awayPlayers.FirstOrDefault(p => p.ShirtNumber == 11)?.MoveTo(new Position(20f, 0f));

        foreach (var p in homePlayers) _playerFacingDir[p.Id] = new Vector2(1f, 0f);
        foreach (var p in awayPlayers)
        {
            _playerFacingDir[p.Id] = new Vector2(-1f, 0f);
            _aiDecisionTimers[p.Id] = 0.4f;
        }

        match.Ball.Reset();
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

        if (match.State == MatchState.GoalScored)
        {
            ResetFormations(match);
            match.ResumePlay();
        }

        match.UpdateTick(dt);
        match.Ball.Update(dt);

        if (match.State == MatchState.InPlay)
        {
            UpdatePossessionEngine(match);
            UpdateGoalkeepers(match);
            UpdateAIFiniteStateMachine(match, dt);
        }

        if (previousState == MatchState.InPlay && match.State == MatchState.GoalScored)
        {
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

    private void UpdatePossessionEngine(Match match)
    {
        var allPlayers = match.HomeTeam.Players.Concat(match.AwayTeam.Players).ToList();

        if (match.Ball.State == BallState.Controlled && match.Ball.Owner != null)
        {
            var possessor = match.Ball.Owner;
            if (!_playerFacingDir.TryGetValue(possessor.Id, out var facing))
            {
                facing = possessor.Position.X >= 0 ? new Vector2(-1f, 0f) : new Vector2(1f, 0f);
            }

            Vector2 footPos = ClampToPitchVector(match, new Vector2(possessor.Position.X + facing.X * 14f, possessor.Position.Y + facing.Y * 14f));
            match.Ball.SetPosition(footPos);

            // Robo por contacto directo de un rival
            foreach (var opponent in allPlayers)
            {
                if (SameTeam(match, possessor, opponent)) continue;
                if (!match.Ball.CanBeCapturedBy(opponent)) continue;

                float dist = Vector2.Distance(new Vector2(opponent.Position.X, opponent.Position.Y), new Vector2(possessor.Position.X, possessor.Position.Y));

                if (dist <= 18f)
                {
                    match.Ball.Attach(opponent);
                    match.Ball.Block(possessor, 1.0f);
                    _soundService?.PlayKickSound();
                    break;
                }
            }
        }
        else
        {
            // Pelota suelta o en pase/tiro
            if (match.Ball.Z <= Ball.ControlHeight)
            {
                float captureRadius = (match.Ball.State == BallState.Free) ? 20f : 14f;

                foreach (var player in allPlayers)
                {
                    if (!match.Ball.CanBeCapturedBy(player)) continue;

                    float dist = Vector2.Distance(new Vector2(player.Position.X, player.Position.Y), match.Ball.Position);
                    if (dist <= captureRadius)
                    {
                        match.Ball.Attach(player);
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

        var homeGk = match.HomeTeam.Players.FirstOrDefault(p => p.ShirtNumber == 1);
        if (homeGk != null)
        {
            homeGk.MoveTo(new Position(-330f, Math.Clamp(ballY, -55f, 55f)));
            float distToBall = Vector2.Distance(new Vector2(homeGk.Position.X, homeGk.Position.Y), match.Ball.Position);

            if (distToBall < 28f && match.Ball.Owner != homeGk)
            {
                ExecuteKickTowards(match, homeGk, new Vector2(150f, (_random.NextSingle() - 0.5f) * 120f), powerSpeed: 360f, vz: 80f, isShot: false);
            }
        }

        var awayGk = match.AwayTeam.Players.FirstOrDefault(p => p.ShirtNumber == 12);
        if (awayGk != null)
        {
            awayGk.MoveTo(new Position(330f, Math.Clamp(ballY, -55f, 55f)));
            float distToBall = Vector2.Distance(new Vector2(awayGk.Position.X, awayGk.Position.Y), match.Ball.Position);

            if (distToBall < 28f && match.Ball.Owner != awayGk)
            {
                ExecuteKickTowards(match, awayGk, new Vector2(-150f, (_random.NextSingle() - 0.5f) * 120f), powerSpeed: 360f, vz: 80f, isShot: false);
            }
        }
    }

    private void UpdateAIFiniteStateMachine(Match match, float deltaTime)
    {
        Vector2 targetBallPos = match.Ball.PredictLanding();
        var awayPlayers = match.AwayTeam.Players.Where(p => p.ShirtNumber != 12).ToList();
        var homePlayers = match.HomeTeam.Players.Where(p => p.ShirtNumber != 1).ToList();

        var controlledPlayerId = GetClosestHomePlayerId(match);

        // --- IA RIVAL (BOCA) ---
        var closestEnemy = awayPlayers.OrderBy(p => Vector2.Distance(new Vector2(p.Position.X, p.Position.Y), targetBallPos)).FirstOrDefault();

        foreach (var enemy in awayPlayers)
        {
            if (match.Ball.Owner == enemy)
            {
                Vector2 attackTarget = new Vector2(-350f, enemy.Position.Y * 0.3f);
                MovePlayerInternal(match, enemy, attackTarget, deltaTime, 0.95f);

                _aiDecisionTimers[enemy.Id] -= deltaTime;
                float distToGoal = Vector2.Distance(new Vector2(enemy.Position.X, enemy.Position.Y), new Vector2(-350f, 0f));
                bool defenderClose = homePlayers.Any(h => Vector2.Distance(new Vector2(h.Position.X, h.Position.Y), new Vector2(enemy.Position.X, enemy.Position.Y)) < 40f);

                if (_aiDecisionTimers[enemy.Id] <= 0f || defenderClose || distToGoal < 220f)
                {
                    // Si está en zona de tiro
                    if (enemy.Position.X < -100f || distToGoal < 210f)
                    {
                        float targetY = (_random.Next(0, 2) == 0) ? -38f : 38f;
                        ExecuteKickTowards(match, enemy, new Vector2(-350f, targetY), powerSpeed: 360f, vz: 30f, isShot: true);
                    }
                    // Si no, da un pase a un compañero desmarcado
                    else
                    {
                        var mate = awayPlayers.FirstOrDefault(p => p.Id != enemy.Id);
                        if (mate != null)
                        {
                            Vector2 matePos = new Vector2(mate.Position.X, mate.Position.Y);
                            ExecuteKickTowards(match, enemy, matePos, powerSpeed: 250f, vz: 10f, isShot: false, passTarget: mate);
                        }
                    }

                    _aiDecisionTimers[enemy.Id] = 0.6f;
                }
            }
            else if (enemy == closestEnemy)
            {
                MovePlayerInternal(match, enemy, targetBallPos, deltaTime, 1.1f);
                _aiDecisionTimers[enemy.Id] = 0.4f;
            }
            else
            {
                float targetY = (enemy.ShirtNumber % 2 == 0) ? 70f : -70f;
                float targetX = Math.Clamp(targetBallPos.X - 45f, -280f, 280f);
                MovePlayerInternal(match, enemy, new Vector2(targetX, targetY), deltaTime, 0.7f);
            }
        }

        // --- DEFICIT Y RECUPERACIÓN DE COMPAÑEROS LOCALES (RIVER) ---
        var closestHomePlayer = homePlayers.OrderBy(p => Vector2.Distance(new Vector2(p.Position.X, p.Position.Y), targetBallPos)).FirstOrDefault();

        foreach (var teammate in homePlayers)
        {
            if (teammate.Id == controlledPlayerId) continue; // Jugador controlado por el usuario

            bool rivalHasBall = match.Ball.Owner != null && !SameTeam(match, teammate, match.Ball.Owner);
            bool ballIsFree = match.Ball.Owner == null;

            if (rivalHasBall || ballIsFree)
            {
                // El compañero más cercano sale a recuperar / presionar la pelota
                if (teammate == closestHomePlayer)
                {
                    MovePlayerInternal(match, teammate, targetBallPos, deltaTime, 1.05f);
                }
                else
                {
                    // Cobertura defensiva de la zona central
                    float targetY = (teammate.ShirtNumber % 2 == 0) ? 50f : -50f;
                    float targetX = Math.Clamp(targetBallPos.X + 30f, -260f, 100f);
                    MovePlayerInternal(match, teammate, new Vector2(targetX, targetY), deltaTime, 0.75f);
                }
            }
            else
            {
                // Ataque: Acompañar el avance abriendo la cancha
                float targetY = (teammate.ShirtNumber % 2 == 0) ? 75f : -75f;
                float targetX = Math.Clamp(targetBallPos.X + 50f, -280f, 280f);
                MovePlayerInternal(match, teammate, new Vector2(targetX, targetY), deltaTime, 0.75f);
            }
        }
    }

    private void MovePlayerInternal(Match match, Player player, Vector2 target, float deltaTime, float speedFactor)
    {
        float dx = target.X - player.Position.X;
        float dy = target.Y - player.Position.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        if (dist > 3f)
        {
            Vector2 dir = Vector2.Normalize(new Vector2(dx, dy));
            _playerFacingDir[player.Id] = dir;

            float speed = player.Stats.Speed * 35f * speedFactor;
            float nx = player.Position.X + (dir.X * speed * deltaTime);
            float ny = player.Position.Y + (dir.Y * speed * deltaTime);

            player.MoveTo(ClampToPitch(match, new Position(nx, ny)));
        }
    }

    private void ExecuteKickTowards(Match match, Player kicker, Vector2 target, float powerSpeed, float vz, bool isShot, Player? passTarget = null)
    {
        Vector2 kickerPos = new Vector2(kicker.Position.X, kicker.Position.Y);
        Vector2 dir = target - kickerPos;
        if (dir.LengthSquared() > 0f) dir = Vector2.Normalize(dir);
        else dir = new Vector2(1f, 0f);

        _playerFacingDir[kicker.Id] = dir;

        BallState targetState = isShot ? BallState.InShot : BallState.InPass;
        match.Ball.Release(kicker, dir * powerSpeed, vz, targetState, blockSeconds: 0.8f, passTarget: passTarget);
        _soundService?.PlayKickSound();
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
            Vector2 dir = Vector2.Normalize(new Vector2(inputX, inputY));
            _playerFacingDir[player.Id] = dir;

            float speed = player.Stats.Speed * 35f;
            float newX = player.Position.X + (dir.X * speed * 0.016f);
            float newY = player.Position.Y + (dir.Y * speed * 0.016f);

            player.MoveTo(ClampToPitch(match, new Position(newX, newY)));
        }
    }

    public void KickBall(KickBallCommand command)
    {
        var match = GetMatchOrThrow(command.MatchId);
        if (match.State != MatchState.InPlay) return;

        var controlledPlayerId = GetClosestHomePlayerId(match);
        var player = match.HomeTeam.Players.FirstOrDefault(p => p.Id == controlledPlayerId);

        if (player != null && (match.Ball.Owner == player || Vector2.Distance(new Vector2(player.Position.X, player.Position.Y), match.Ball.Position) < 24f))
        {
            float powerSpeed = Math.Clamp(command.Power * Ball.PowerToSpeed, 180f, 380f);
            float vz = command.Power > 65f ? (command.Power - 65f) * 4f : 0f;

            if (command.Power < 50f)
            {
                var teammate = match.HomeTeam.Players
                    .Where(p => p.ShirtNumber != 1 && p.Id != player.Id)
                    .OrderBy(p => Vector2.Distance(new Vector2(p.Position.X, p.Position.Y), new Vector2(player.Position.X, player.Position.Y)))
                    .FirstOrDefault();

                if (teammate != null)
                {
                    ExecuteKickTowards(match, player, new Vector2(teammate.Position.X, teammate.Position.Y), powerSpeed: 250f, vz: 10f, isShot: false, passTarget: teammate);
                    return;
                }
            }

            Vector2 dir = new Vector2(command.DirectionX, command.DirectionY);
            if (dir.X == 0f && dir.Y == 0f) dir = new Vector2(1f, (_random.Next(0, 2) == 0) ? -0.2f : 0.2f);

            Vector2 target = new Vector2(player.Position.X, player.Position.Y) + dir * 120f;
            ExecuteKickTowards(match, player, target, powerSpeed, vz, isShot: true);
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
            .OrderBy(p => Vector2.Distance(new Vector2(p.Position.X, p.Position.Y), match.Ball.PredictLanding()))
            .First().Id;
    }

    private static Position ClampToPitch(Match match, Position position)
    {
        float halfWidth = match.PitchWidth / 2f - 12f;
        float halfHeight = match.PitchHeight / 2f - 12f;
        return new Position(Math.Clamp(position.X, -halfWidth, halfWidth), Math.Clamp(position.Y, -halfHeight, halfHeight));
    }

    private static Vector2 ClampToPitchVector(Match match, Vector2 position)
    {
        float halfWidth = match.PitchWidth / 2f - 12f;
        float halfHeight = match.PitchHeight / 2f - 12f;
        return new Vector2(Math.Clamp(position.X, -halfWidth, halfWidth), Math.Clamp(position.Y, -halfHeight, halfHeight));
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
            match.Ball.Z,
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