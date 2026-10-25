using System.Numerics;

namespace SoccerGame.Domain.Entities;

public enum BallState
{
    Free,
    Controlled,
    InPass,
    InShot
}

public sealed class Ball
{
    public const float Friction = 2.5f;
    public const float Gravity = 300f;
    public const float AirDrag = 0.15f;
    public const float BounceRestitution = 0.45f;
    public const float PowerToSpeed = 20f;
    public const float ControlHeight = 16f;

    private const float StopSpeed = 35f;

    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public float Z { get; set; }
    public float VZ { get; set; }

    public BallState State { get; private set; } = BallState.Free;
    public Player? Owner { get; private set; }
    public Player? PassTarget { get; private set; }
    public Player? LastToucher { get; private set; }

    public Player? BlockedPlayer { get; private set; }
    public float BlockedTimer { get; private set; }

    public bool IsAirborne => Z > 0.5f;
    public float Speed => Velocity.Length();

    public event Action? Bounced;

    public Ball(Vector2 initialPosition)
    {
        Position = initialPosition;
        Velocity = Vector2.Zero;
        Z = 0f;
        VZ = 0f;
    }

    public void Reset()
    {
        Position = Vector2.Zero;
        Velocity = Vector2.Zero;
        Z = 0f;
        VZ = 0f;
        State = BallState.Free;
        Owner = null;
        PassTarget = null;
        LastToucher = null;
        BlockedPlayer = null;
        BlockedTimer = 0f;
    }

    public void SetPosition(Vector2 position)
    {
        Position = position;
    }

    public void Attach(Player player)
    {
        Owner = player;
        LastToucher = player;
        PassTarget = null;
        State = BallState.Controlled;
        Velocity = Vector2.Zero;
        Z = 0f;
        VZ = 0f;
    }

    public void Release(Player kicker, Vector2 velocity, float vz, BallState state, float blockSeconds, Player? passTarget = null)
    {
        Owner = null;
        State = state;
        Velocity = velocity;
        VZ = vz;
        if (vz > 0f && Z < 0.5f) Z = 0.5f;
        PassTarget = passTarget;
        LastToucher = kicker;
        Block(kicker, blockSeconds);
    }

    public void Block(Player player, float seconds)
    {
        BlockedPlayer = player;
        BlockedTimer = seconds;
    }

    public bool CanBeCapturedBy(Player player) => BlockedTimer <= 0f || BlockedPlayer != player;

    public void SetFree()
    {
        Owner = null;
        PassTarget = null;
        State = BallState.Free;
    }

    public Vector2 PredictLanding()
    {
        if (State == BallState.Controlled) return Position;
        if (Z > 0.5f || VZ > 0f)
        {
            float t = (VZ + MathF.Sqrt(MathF.Max(0f, VZ * VZ + 2f * Gravity * Z))) / Gravity;
            return Position + Velocity * t * (1f - AirDrag * t * 0.5f);
        }
        return Position + Velocity * 0.25f;
    }

    public void Update(float dt)
    {
        if (BlockedTimer > 0f)
        {
            BlockedTimer -= dt;
            if (BlockedTimer <= 0f)
            {
                BlockedTimer = 0f;
                BlockedPlayer = null;
            }
        }

        if (State == BallState.Controlled) return;

        // --- Eje vertical (Z): Gravedad y Rebote ---
        if (Z > 0f || VZ > 0f)
        {
            VZ -= Gravity * dt;
            Z += VZ * dt;
            if (Z <= 0f)
            {
                Z = 0f;
                if (MathF.Abs(VZ) > 45f)
                {
                    VZ = -VZ * BounceRestitution;
                    Velocity *= 0.82f;
                    Bounced?.Invoke();
                }
                else
                {
                    VZ = 0f;
                }
            }
        }

        // --- Plano horizontal: Rozamiento según superficie/aire ---
        bool rolling = Z <= 0f && VZ <= 0f;
        float k = rolling ? Friction : AirDrag;
        Velocity *= MathF.Max(0f, 1f - k * dt);
        Position += Velocity * dt;

        if (rolling && Velocity.LengthSquared() < 16f) Velocity = Vector2.Zero;

        if (State != BallState.Free && rolling && Speed < StopSpeed)
        {
            State = BallState.Free;
            PassTarget = null;
        }
    }
}