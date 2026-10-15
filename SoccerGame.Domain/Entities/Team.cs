using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoccerGame.Domain.Entities;

public class Team
{
    private readonly List<Player> _players = new();

    public Guid Id { get; }
    public string Name { get; }
    public IReadOnlyCollection<Player> Players => _players.AsReadOnly();

    public Team(Guid id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del equipo no puede estar vacío.", nameof(name));

        Id = id;
        Name = name;
    }

    public void AddPlayer(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (_players.Any(p => p.ShirtNumber == player.ShirtNumber))
            throw new InvalidOperationException($"Ya existe un jugador con el número {player.ShirtNumber} en este equipo.");

        _players.Add(player);
    }
}