using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SoccerGame.Domain.ValueObjects;

using SoccerGame.Domain.ValueObjects;

namespace SoccerGame.Domain.Entities;

public class Player
{
    public Guid Id { get; }
    public string Name { get; }
    public int ShirtNumber { get; }
    public Position Position { get; private set; }
    public PlayerStats Stats { get; }

    public Player(Guid id, string name, int shirtNumber, Position initialPosition, PlayerStats stats)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del jugador no puede estar vacío.", nameof(name));

        if (shirtNumber <= 0 || shirtNumber > 99)
            throw new ArgumentOutOfRangeException(nameof(shirtNumber), "El número de camiseta debe estar entre 1 y 99.");

        Id = id;
        Name = name;
        ShirtNumber = shirtNumber;
        Position = initialPosition;
        Stats = stats;
    }

    public void MoveTo(Position newPosition) => Position = newPosition;
}