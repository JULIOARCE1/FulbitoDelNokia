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
        // 1. Inicializar la ventana estilo retro
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "Soccer Game Retro - Nokia Style");
        Raylib.SetTargetFPS(60); // Game Loop a 60 FPS

        // 2. Instanciar servicio de aplicación y crear un partido de prueba
        var matchService = new MatchApplicationService();
        var matchId = matchService.CreateMatch("River Plate", "Boca Juniors", durationSeconds: 120f);
        matchService.StartMatch(new StartMatchCommand(matchId));

        // 3. Bucle principal del juego (Game Loop)
        while (!Raylib.WindowShouldClose())
        {
            float deltaTime = Raylib.GetFrameTime();

            // --- A. CAPTURA DE INPUT ---
            float inputX = 0f;
            float inputY = 0f;

            if (Raylib.IsKeyDown(KeyboardKey.Right) || Raylib.IsKeyDown(KeyboardKey.D)) inputX += 1f;
            if (Raylib.IsKeyDown(KeyboardKey.Left) || Raylib.IsKeyDown(KeyboardKey.A)) inputX -= 1f;
            if (Raylib.IsKeyDown(KeyboardKey.Down) || Raylib.IsKeyDown(KeyboardKey.S)) inputY += 1f;
            if (Raylib.IsKeyDown(KeyboardKey.Up) || Raylib.IsKeyDown(KeyboardKey.W)) inputY -= 1f;

            // Enviar comando de movimiento si hubo input
            if (inputX != 0f || inputY != 0f)
            {
                // Por ahora usamos el MatchId directamente
                // En la siguiente etapa se mapeara al jugador activo
            }

            // --- B. ACTUALIZACIÓN DE LA SIMULACIÓN ---
            matchService.UpdateTick(new UpdateTickCommand(matchId, deltaTime));

            // Obtener estado plano de lectura
            var status = matchService.GetMatchStatus(matchId);

            // --- C. RENDERIZADO (DRAW) ---
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(34, 139, 34, 255)); // Verde cancha de fútbol

            if (status != null)
            {
                // Dibujar Cancha básica (Líneas blancas)
                Raylib.DrawRectangleLines(50, 50, ScreenWidth - 100, ScreenHeight - 100, Color.White);
                Raylib.DrawLine(ScreenWidth / 2, 50, ScreenWidth / 2, ScreenHeight - 50, Color.White);
                Raylib.DrawCircleLines(ScreenWidth / 2, ScreenHeight / 2, 60, Color.White);

                // Dibujar Pelota (Círculo blanco)
                // Mapeamos las coordenadas de la simulación a pantalla (offset del centro)
                int ballScreenX = (ScreenWidth / 2) + (int)status.BallX;
                int ballScreenY = (ScreenHeight / 2) + (int)status.BallY;
                Raylib.DrawCircle(ballScreenX, ballScreenY, 6, Color.White);

                // Dibujar Marcador HUD en pantalla
                string scoreText = $"{status.HomeTeamName} {status.HomeGoals} - {status.AwayGoals} {status.AwayTeamName}";
                string timeText = $"Tiempo: {(int)status.ElapsedTimeInSeconds}s | Estado: {status.State}";

                Raylib.DrawText(scoreText, ScreenWidth / 2 - Raylib.MeasureText(scoreText, 20) / 2, 15, 20, Color.Yellow);
                Raylib.DrawText(timeText, 60, ScreenHeight - 35, 16, Color.RayWhite);
            }

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}