using Raylib_cs;
using SoccerGame.Application.Commands;
using SoccerGame.Application.Services;
using SoccerGame.Infrastructure.Audio;
using SoccerGame.Infrastructure.Persistence;

namespace SoccerGame.Presentation;

public static class Program
{
    private const int ScreenWidth = 800;
    private const int ScreenHeight = 600;

    public static void Main()
    {
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "Soccer Game Retro - Nokia Style");
        Raylib.SetTargetFPS(60);

        var soundService = new RaylibSoundService();
        var matchRepository = new JsonMatchRepository();
        var matchService = new MatchApplicationService(soundService, matchRepository);

        var matchId = matchService.CreateMatch("River Plate", "Boca Juniors", durationSeconds: 180f);
        matchService.StartMatch(new StartMatchCommand(matchId));

        while (!Raylib.WindowShouldClose())
        {
            float deltaTime = Raylib.GetFrameTime();
            var status = matchService.GetMatchStatus(matchId);

            if (status != null && status.State == "InPlay" && status.Players.Any())
            {
                var controlledPlayer = status.Players
                    .Where(p => p.IsHomeTeam && p.ShirtNumber != 1)
                    .OrderBy(p => MathF.Sqrt(MathF.Pow(p.X - status.BallX, 2) + MathF.Pow(p.Y - status.BallY, 2)))
                    .First();

                float inputX = 0f;
                float inputY = 0f;

                if (Raylib.IsKeyDown(KeyboardKey.Right) || Raylib.IsKeyDown(KeyboardKey.D)) inputX += 1f;
                if (Raylib.IsKeyDown(KeyboardKey.Left) || Raylib.IsKeyDown(KeyboardKey.A)) inputX -= 1f;
                if (Raylib.IsKeyDown(KeyboardKey.Down) || Raylib.IsKeyDown(KeyboardKey.S)) inputY += 1f;
                if (Raylib.IsKeyDown(KeyboardKey.Up) || Raylib.IsKeyDown(KeyboardKey.W)) inputY -= 1f;

                if (inputX != 0f || inputY != 0f)
                {
                    matchService.MovePlayer(new MovePlayerCommand(matchId, controlledPlayer.Id, inputX, inputY));
                }

                if (Raylib.IsKeyPressed(KeyboardKey.Space))
                {
                    matchService.KickBall(new KickBallCommand(matchId, controlledPlayer.Id, inputX, inputY, 75f));
                }

                if (Raylib.IsKeyPressed(KeyboardKey.X))
                {
                    matchService.KickBall(new KickBallCommand(matchId, controlledPlayer.Id, inputX, inputY, 45f));
                }
            }

            matchService.UpdateTick(new UpdateTickCommand(matchId, deltaTime));

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(34, 139, 34, 255));

            if (status != null)
            {
                int centerX = ScreenWidth / 2;
                int centerY = ScreenHeight / 2;

                Raylib.DrawRectangleLines(50, 50, ScreenWidth - 100, ScreenHeight - 100, Color.White);
                Raylib.DrawLine(centerX, 50, centerX, ScreenHeight - 50, Color.White);
                Raylib.DrawCircleLines(centerX, centerY, 60, Color.White);

                Raylib.DrawRectangleLines(30, centerY - 60, 20, 120, Color.White);
                Raylib.DrawRectangleLines(ScreenWidth - 50, centerY - 60, 20, 120, Color.White);

                var activePlayer = status.Players
                    .Where(p => p.IsHomeTeam && p.ShirtNumber != 1)
                    .OrderBy(p => MathF.Sqrt(MathF.Pow(p.X - status.BallX, 2) + MathF.Pow(p.Y - status.BallY, 2)))
                    .FirstOrDefault();

                foreach (var player in status.Players)
                {
                    int px = centerX + (int)player.X;
                    int py = centerY + (int)player.Y;

                    bool isGk = player.ShirtNumber == 1 || player.ShirtNumber == 12;
                    Color teamColor = isGk ? Color.Orange : (player.IsHomeTeam ? Color.Red : Color.Blue);

                    if (activePlayer != null && player.Id == activePlayer.Id)
                    {
                        Raylib.DrawTriangle(
                            new System.Numerics.Vector2(px, py - 18),
                            new System.Numerics.Vector2(px - 5, py - 26),
                            new System.Numerics.Vector2(px + 5, py - 26),
                            Color.Yellow
                        );
                    }

                    Raylib.DrawCircle(px, py, 10, teamColor);
                    Raylib.DrawText(player.ShirtNumber.ToString(), px - 3, py - 4, 10, Color.White);
                }

                int ballX = centerX + (int)status.BallX;
                int ballY = centerY + (int)status.BallY;
                Raylib.DrawCircle(ballX, ballY, 5, Color.White);

                string scoreText = $"{status.HomeTeamName} {status.HomeGoals} - {status.AwayGoals} {status.AwayTeamName}";
                string hudText = $"Tiempo: {(int)status.ElapsedTimeInSeconds}s | Flechas (Mover) - ESPACIO (Patear) - X (Pase)";

                Raylib.DrawText(scoreText, centerX - Raylib.MeasureText(scoreText, 20) / 2, 15, 20, Color.Yellow);
                Raylib.DrawText(hudText, 30, ScreenHeight - 35, 14, Color.RayWhite);

                if (status.State == "GoalScored")
                {
                    Raylib.DrawText("¡¡¡ GOOOL !!!", centerX - 100, centerY - 20, 40, Color.Gold);
                }

                if (status.State == "Finished")
                {
                    Raylib.DrawText("PARTIDO FINALIZADO", centerX - 140, centerY - 20, 26, Color.Red);
                }
            }

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}