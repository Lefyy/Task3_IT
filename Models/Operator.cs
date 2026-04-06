using System;

namespace Task3.Models;

public class Operator
{
    public event Action<Quadcopter>? ControllerTurnedOn;

    public void TurnOnController(Quadcopter quadcopter)
    {
        ControllerTurnedOn?.Invoke(quadcopter);
    }
}
