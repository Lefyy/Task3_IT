using System;
using System.Threading;
using System.Threading.Tasks;

namespace Task3.Models;

public delegate void RepairMessageHandler(string message);

public class Mechanic : IMechanic
{
    public event RepairMessageHandler? RepairMessagePublished;

    public async Task RepairAsync(Quadcopter quadcopter, CancellationToken cancellationToken)
    {
        RepairMessagePublished?.Invoke($"Механик начал ремонт дрона #{quadcopter.Id}.");
        quadcopter.State = DroneState.Repairing;
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        quadcopter.RestoreGps();
        quadcopter.State = DroneState.Idle;
        RepairMessagePublished?.Invoke($"Дрон #{quadcopter.Id} отремонтирован, GPS восстановлен.");
    }
}
