using Raylib_cs;
using SoccerGame.Application.Commands;
using SoccerGame.Application.Services;

namespace SoccerGame.Presentation;

public static class Program
{
    private const int ScreenWidth = 800;
    private const int ScreenHeight = 600;

    public static void Main()
    {
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "Soccer Game Retro - Nokia Style (Etapa 4)");
        Raylib.SetTargetFPS(60);

        var matchService = new MatchApplicationService();
        var matchId = matchService.CreateMatch("River Plate", "Boca Juniors", durationSeconds: 180f);
        matchService.StartMatch(new StartMatchCommand(matchId));

        while (!Raylib.WindowShouldClose())
        {
            float deltaTime = Raylib.GetFrameTime();
            var status = matchService.GetMatchStatus(matchId);

            if (status != null && status.Players.Any())
            {
                // Seleccionar al primer jugador local como nuestro jugador controlado
                var controlledPlayer = status.Players.First(p => p.IsHomeTeam);

                // --- A. CAPTURA DE INPUT ---
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

                // Tecla Espacio para Patear hacia la derecha (hacia el arco rival)
                if (Raylib.IsKeyPressed(KeyboardKey.Space))
                {
                    matchService.KickBall(new KickBallCommand(matchId, DirectionX: 1f, DirectionY: 0f, Power: 50f));
                }
            }

            // --- B. ACTUALIZACIÓN DE LA SIMULACIÓN ---
            matchService.UpdateTick(new UpdateTickCommand(matchId, deltaTime));

            // --- C. RENDERIZADO (DRAW) ---
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(34, 139, 34, 255)); // Campo Verde

            if (status != null)
            {
                int centerX = ScreenWidth / 2;
                int centerY = ScreenHeight / 2;

                // Dibujar Cancha y Arcos estilo Nokia
                Raylib.DrawRectangleLines(50, 50, ScreenWidth - 100, ScreenHeight - 100, Color.White);
                Raylib.DrawLine(centerX, 50, centerX, ScreenHeight - 50, Color.White);
                Raylib.DrawCircleLines(centerX, centerY, 60, Color.White);

                // Arco Izquierdo y Derecho
                Raylib.DrawRectangleLines(30, centerY - 60, 20, 120, Color.White);
                Raylib.DrawRectangleLines(ScreenWidth - 50, centerY - 60, 20, 120, Color.White);

                // Dibujar Jugadores
                foreach (var player in status.Players)
                {
                    int px = centerX + (int)player.X;
                    int py = centerY + (int)player.Y;
                    Color teamColor = player.IsHomeTeam ? Color.Red : Color.Blue;

                    // Dibujar cuerpo del jugador
                    Raylib.DrawCircle(px, py, 10, teamColor);
                    Raylib.DrawText(player.ShirtNumber.ToString(), px - 3, py - 4, 10, Color.White);
                }

                // Dibujar Pelota
                int ballX = centerX + (int)status.BallX;
                int ballY = centerY + (int)status.BallY;
                Raylib.DrawCircle(ballX, ballY, 5, Color.White);

                // Marcador e Indicaciones
                string scoreText = $"{status.HomeTeamName} {status.HomeGoals} - {status.AwayGoals} {status.AwayTeamName}";
                string hudText = $"Tiempo: {(int)status.ElapsedTimeInSeconds}s | Teclas: WASD/Flechas (Mover) - ESPACIO (Patear)";

                Raylib.DrawText(scoreText, centerX - Raylib.MeasureText(scoreText, 20) / 2, 15, 20, Color.Yellow);
                Raylib.DrawText(hudText, 50, ScreenHeight - 35, 14, Color.RayWhite);

                if (status.State == "GoalScored")
                {
                    Raylib.DrawText("¡¡¡ GOOOL !!!", centerX - 100, centerY - 20, 40, Color.Gold);
                }
            }

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}