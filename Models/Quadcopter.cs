using System;
using System.Threading;
using System.Threading.Tasks;

namespace Task3.Models;

public class Quadcopter
{
    private const int FlightTickDelayMs = 40;
    private const double DefaultGpsFailureProbabilityPerSecond = 0.03;
    private readonly Random _random;
    private readonly object _sync = new();
    private CancellationTokenSource? _loopCts;
    private CancellationTokenSource? _emergencyCts;
    private int _flightSessionId;

    public Quadcopter(int id, double initialX, double initialY, double areaWidth, double areaHeight, Random random)
    {
        Id = id;
        X = initialX;
        Y = initialY;
        AreaWidth = areaWidth;
        AreaHeight = areaHeight;
        _random = random;
    }

    public int Id { get; }

    public double X { get; private set; }

    public double Y { get; private set; }

    public double VelocityX { get; private set; }

    public double VelocityY { get; private set; }

    public double AreaWidth { get; }

    public double AreaHeight { get; }

    public bool GpsEnabled { get; private set; } = true;

    public double GpsFailureProbabilityPerSecond { get; } = DefaultGpsFailureProbabilityPerSecond;

    public DroneState State { get; private set; } = DroneState.Idle;

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
        lock (_sync)
        {
            if (State is DroneState.Flying or DroneState.Landing or DroneState.Repairing || !GpsEnabled)
            {
                return;
            }

            SetRandomVelocity();
            _flightSessionId++;
            State = DroneState.Flying;
        }

        StartLoop();
    }

    public void StopSimulation()
    {
        _loopCts?.Cancel();
        _emergencyCts?.Cancel();
        lock (_sync)
        {
            State = DroneState.Idle;
            _flightSessionId++;
        }
    }

    public void SetState(DroneState state)
    {
        lock (_sync)
        {
            State = state;
        }

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
            SetRandomVelocity();
        }
    }

    private void SetRandomVelocity()
    {
        VelocityX = _random.NextDouble() * 2 + 1;
        VelocityY = _random.NextDouble() * 2 + 1;
    }

    private void OnControllerTurnedOn(Quadcopter quadcopter)
    {
        if (!ReferenceEquals(this, quadcopter))
        {
            return;
        }

        var snapshot = GetSnapshot();
        if (snapshot.State is DroneState.Flying or DroneState.Landing or DroneState.Repairing || !snapshot.GpsEnabled)
        {
            return;
        }
        StartFlight();
    }

    private void StartLoop()
    {
        _loopCts?.Cancel();
        _loopCts = new CancellationTokenSource();
        var token = _loopCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (TryTriggerGpsFailureDuringFlight())
                    {
                        await Task.Delay(FlightTickDelayMs, token);
                        continue;
                    }

                    UpdatePosition();
                    await Task.Delay(FlightTickDelayMs, token);
                }
            }
            catch (TaskCanceledException)
            {
                // ignored
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

    private bool TryTriggerGpsFailureDuringFlight()
    {
        var shouldTriggerEmergencyLanding = false;
        var sessionId = 0;

        lock (_sync)
        {
            var dtSeconds = FlightTickDelayMs / 1000d;
            var gpsFailureProbabilityPerTick = 1 - Math.Pow(1 - GpsFailureProbabilityPerSecond, dtSeconds);

            if (State == DroneState.Flying && GpsEnabled && _random.NextDouble() <= gpsFailureProbabilityPerTick)
            {
                shouldTriggerEmergencyLanding = true;
                sessionId = _flightSessionId;
            }

        }

        if (!shouldTriggerEmergencyLanding)
        {
            return false;
        }

        TriggerEmergencyLanding(sessionId);
        return true;
    }

    private void TriggerEmergencyLanding(int sessionId)
    {
        _loopCts?.Cancel();
        _emergencyCts?.Cancel();
        _emergencyCts = new CancellationTokenSource();
        var emergencyToken = _emergencyCts.Token;

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
            try
            {
                lock (_sync)
                {
                    State = DroneState.Landing;
                }

                await Task.Delay(1000, emergencyToken);
                lock (_sync)
                {
                    if (_flightSessionId != sessionId)
                    {
                        return;
                    }

                    State = DroneState.Idle;
                }

                Landed?.Invoke(this);
            }
            catch (TaskCanceledException)
            {
               // ignored
            }
        });
    }
}
