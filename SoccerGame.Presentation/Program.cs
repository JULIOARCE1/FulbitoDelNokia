using System.Numerics;
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
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "Soccer Game Retro - Nokia J2ME 2.5D Style");
        Raylib.SetTargetFPS(60);

        var soundService = new RaylibSoundService();
        var matchRepository = new JsonMatchRepository();
        var matchService = new MatchApplicationService(soundService, matchRepository);

        var matchId = matchService.CreateMatch("River Plate", "Boca Juniors", durationSeconds: 180f);
        matchService.StartMatch(new StartMatchCommand(matchId));

        float powerCharge = 0f;

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

                // CARGA DE POTENCIA CON ESPACIO O TECLA X/H
                if (Raylib.IsKeyDown(KeyboardKey.Space) || Raylib.IsKeyDown(KeyboardKey.X) || Raylib.IsKeyDown(KeyboardKey.H))
                {
                    powerCharge = MathF.Min(100f, powerCharge + deltaTime * 120f);
                }

                if (Raylib.IsKeyReleased(KeyboardKey.Space))
                {
                    float power = MathF.Max(35f, powerCharge);
                    matchService.KickBall(new KickBallCommand(matchId, controlledPlayer.Id, inputX, inputY, power, IsPass: false));
                    powerCharge = 0f;
                }

                // TECLA X / H: Pase Corto Asistido a Compañero
                if (Raylib.IsKeyReleased(KeyboardKey.X) || Raylib.IsKeyReleased(KeyboardKey.H))
                {
                    matchService.KickBall(new KickBallCommand(matchId, controlledPlayer.Id, inputX, inputY, 35f, IsPass: true));
                    powerCharge = 0f;
                }
            }

            matchService.UpdateTick(new UpdateTickCommand(matchId, deltaTime));

            Raylib.BeginDrawing();

            Raylib.ClearBackground(new Color(28, 105, 30, 255));
            int stripeWidth = 50;
            for (int x = 40; x < ScreenWidth - 40; x += stripeWidth * 2)
            {
                Raylib.DrawRectangle(x, 40, stripeWidth, ScreenHeight - 80, new Color(34, 122, 36, 255));
            }

            if (status != null)
            {
                int centerX = ScreenWidth / 2;
                int centerY = ScreenHeight / 2;

                Color lineCol = new Color(240, 240, 240, 255);
                Raylib.DrawRectangleLinesEx(new Rectangle(40, 40, ScreenWidth - 80, ScreenHeight - 80), 3, lineCol);
                Raylib.DrawLineEx(new Vector2(centerX, 40), new Vector2(centerX, ScreenHeight - 40), 3, lineCol);
                Raylib.DrawCircleLines(centerX, centerY, 65, lineCol);

                Raylib.DrawRectangleLinesEx(new Rectangle(40, centerY - 100, 90, 200), 2, lineCol);
                Raylib.DrawRectangleLinesEx(new Rectangle(ScreenWidth - 130, centerY - 100, 90, 200), 2, lineCol);

                var activePlayer = status.Players
                    .Where(p => p.IsHomeTeam && p.ShirtNumber != 1)
                    .OrderBy(p => MathF.Sqrt(MathF.Pow(p.X - status.BallX, 2) + MathF.Pow(p.Y - status.BallY, 2)))
                    .FirstOrDefault();

                foreach (var player in status.Players)
                {
                    int px = centerX + (int)player.X;
                    int py = centerY + (int)player.Y;

                    Raylib.DrawEllipse(px + 2, py + 10, 10, 4, new Color(0, 0, 0, 80));

                    bool isGk = player.ShirtNumber == 1 || player.ShirtNumber == 12;
                    Color bodyColor = isGk ? new Color(240, 140, 20, 255) : (player.IsHomeTeam ? new Color(210, 35, 35, 255) : new Color(30, 80, 210, 255));

                    if (activePlayer != null && player.Id == activePlayer.Id)
                    {
                        Raylib.DrawTriangle(
                            new Vector2(px, py - 20),
                            new Vector2(px - 6, py - 28),
                            new Vector2(px + 6, py - 28),
                            Color.Yellow
                        );

                        // BARRA VISUAL DE POTENCIA
                        if (powerCharge > 0f)
                        {
                            Raylib.DrawRectangle(px - 15, py + 16, 30, 5, Color.DarkGray);
                            Raylib.DrawRectangle(px - 15, py + 16, (int)(30 * (powerCharge / 100f)), 5, Color.Red);
                        }
                    }

                    Raylib.DrawCircle(px, py, 11, Color.Black);
                    Raylib.DrawCircle(px, py, 9, bodyColor);
                    Raylib.DrawText(player.ShirtNumber.ToString(), px - 3, py - 5, 10, Color.White);
                }

                // RENDERIZADO 2.5D: SOMBRA EN EL PISO + ELEVACIÓN POR EJE Z
                int ballGroundX = centerX + (int)status.BallX;
                int ballGroundY = centerY + (int)status.BallY;
                int ballAirY = ballGroundY - (int)(status.BallZ * 0.45f);

                // Sombra en el césped (se encoge al subir)
                int shadowRadius = Math.Max(2, 6 - (int)(status.BallZ * 0.08f));
                Raylib.DrawEllipse(ballGroundX, ballGroundY + 4, shadowRadius * 2, shadowRadius, new Color(0, 0, 0, 120));

                // Cuerpo de la Pelota elevándose
                Raylib.DrawCircle(ballGroundX, ballAirY, 6, Color.Black);
                Raylib.DrawCircle(ballGroundX, ballAirY, 5, Color.White);

                // HUD
                Raylib.DrawRectangle(0, 0, ScreenWidth, 38, new Color(15, 20, 25, 230));
                string scoreText = $"{status.HomeTeamName.ToUpper()}  {status.HomeGoals} - {status.AwayGoals}  {status.AwayTeamName.ToUpper()}";
                Raylib.DrawText(scoreText, centerX - Raylib.MeasureText(scoreText, 18) / 2, 10, 18, Color.Yellow);

                if (status.State == "GoalScored")
                {
                    Raylib.DrawRectangle(centerX - 180, centerY - 45, 360, 80, new Color(0, 0, 0, 220));
                    Raylib.DrawText("¡¡¡ GOOOOOOL !!!", centerX - Raylib.MeasureText("¡¡¡ GOOOOOOL !!!", 32) / 2, centerY - 20, 32, Color.Gold);
                }
            }

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}