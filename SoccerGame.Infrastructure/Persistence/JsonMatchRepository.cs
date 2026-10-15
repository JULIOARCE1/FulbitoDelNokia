using System;
using System.Collections.Generic;
using System.Text;

using System.Text.Json;
using SoccerGame.Application.DTOs;
using SoccerGame.Application.Interfaces;

namespace SoccerGame.Infrastructure.Persistence;

public class JsonMatchRepository : IMatchRepository
{
    private readonly string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "match_history.json");

    public async Task SaveMatchHistoryAsync(MatchStatusDto matchStatus)
    {
        List<MatchStatusDto> history = new();

        if (File.Exists(_filePath))
        {
            string existingJson = await File.ReadAllTextAsync(_filePath);
            history = JsonSerializer.Deserialize<List<MatchStatusDto>>(existingJson) ?? new List<MatchStatusDto>();
        }

        history.Add(matchStatus);
        string json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_filePath, json);
    }
}