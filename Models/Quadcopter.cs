using System;
using System.Threading;
using System.Threading.Tasks;

namespace Task3.Models;

public class Quadcopter
{
    private readonly Random _random;
    private readonly object _sync = new();
    private CancellationTokenSource? _loopCts;

    public Quadcopter(int id, double initialX, double initialY, double areaWidth, double areaHeight, Random random)
    {
        Id = id;
        X = initialX;
        Y = initialY;
        AreaWidth = areaWidth;
        AreaHeight = areaHeight;
        _random = random;
        VelocityX = _random.NextDouble() * 2 + 1;
        VelocityY = _random.NextDouble() * 2 + 1;
        GpsFailureProbability = 0.3;
        State = DroneState.Idle;
    }

    public int Id { get; }

    public double X { get; private set; }

    public double Y { get; private set; }

    public double VelocityX { get; private set; }

    public double VelocityY { get; private set; }

    public double AreaWidth { get; }

    public double AreaHeight { get; }

    public bool GpsEnabled { get; private set; } = true;

    public double GpsFailureProbability { get; }

    public DroneState State { get; set; }

    public event Action<Quadcopter>? PositionChanged;

    public event Action<Quadcopter>? GpsDisabled;

    public event Action<Quadcopter>? EmergencyLandingStarted;

    public event Action<Quadcopter>? Landed;

    public void SubscribeToOperator(Operator @operator)
    {
        @operator.ControllerTurnedOn += OnControllerTurnedOn;
    }

    public void StartFlight()
    {
        if (State is DroneState.Flying or DroneState.Landing or DroneState.Repairing)
        {
            return;
        }

        State = DroneState.Flying;
        StartLoop();
    }

    public void StopSimulation()
    {
        _loopCts?.Cancel();
    }

    public (double X, double Y, DroneState State, bool GpsEnabled) GetSnapshot()
    {
        lock (_sync)
        {
            return (X, Y, State, GpsEnabled);
        }
    }

    public void RestoreGps()
    {
        lock (_sync)
        {
            GpsEnabled = true;
            VelocityX = _random.NextDouble() * 2 + 1;
            VelocityY = _random.NextDouble() * 2 + 1;
        }
    }

    private void OnControllerTurnedOn(Quadcopter quadcopter)
    {
        if (!ReferenceEquals(this, quadcopter))
        {
            return;
        }

        if (_random.NextDouble() <= GpsFailureProbability)
        {
            TriggerEmergencyLanding();
        }
    }

    private void StartLoop()
    {
        _loopCts?.Cancel();
        _loopCts = new CancellationTokenSource();
        var token = _loopCts.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                UpdatePosition();
                await Task.Delay(40, token);
            }
        }, token);
    }

    private void UpdatePosition()
    {
        lock (_sync)
        {
            if (State != DroneState.Flying)
            {
                return;
            }

            X += VelocityX;
            Y += VelocityY;

            if (X <= 0 || X >= AreaWidth - 20)
            {
                VelocityX = -VelocityX;
                X = Math.Clamp(X, 0, AreaWidth - 20);
            }

            if (Y <= 0 || Y >= AreaHeight - 20)
            {
                VelocityY = -VelocityY;
                Y = Math.Clamp(Y, 0, AreaHeight - 20);
            }
        }

        PositionChanged?.Invoke(this);
    }

    private void TriggerEmergencyLanding()
    {
        lock (_sync)
        {
            GpsEnabled = false;
            State = DroneState.Emergency;
            VelocityX = 0;
            VelocityY = 0;
        }

        GpsDisabled?.Invoke(this);
        EmergencyLandingStarted?.Invoke(this);

        _ = Task.Run(async () =>
        {
            lock (_sync)
            {
                State = DroneState.Landing;
            }

            await Task.Delay(1000);
            lock (_sync)
            {
                State = DroneState.Idle;
            }

            Landed?.Invoke(this);
        });
    }
}
