using System.Threading;
using System.Threading.Tasks;

namespace Task3.Models;

public interface IMechanic
{
    Task RepairAsync(Quadcopter quadcopter, CancellationToken cancellationToken);
}
