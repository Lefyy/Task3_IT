using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Task3.Models;

namespace Task3.ViewModels;


public partial class MainWindowViewModel : ViewModelBase
{
    private readonly Random _random = new();
    private readonly DispatcherTimer _timer;
    private int _droneCounter;

    public MainWindowViewModel()
    {
        AddDroneCommand = new RelayCommand(AddDrone);

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(40)
        };

        _timer.Tick += (_, _) =>
        {
            foreach (var drone in Drones)
            {
                drone.SyncFromModel();
            }
        };

        _timer.Start();
    }

    public ObservableCollection<DroneViewModel> Drones { get; } = new();

    public RelayCommand AddDroneCommand { get; }

    private void AddDrone()
    {
        _droneCounter++;

        var quadcopter = new Quadcopter(
            id: _droneCounter,
            initialX: _random.Next(20, 760),
            initialY: _random.Next(20, 420),
            areaWidth: 780,
            areaHeight: 440,
            random: _random);

        var @operator = new Operator();
        quadcopter.SubscribeToOperator(@operator);

        var mechanic = CreateMechanicWithReflection();
        var droneVm = new DroneViewModel(quadcopter, @operator, mechanic);

        Drones.Add(droneVm);
        droneVm.Start();
    }

    private void StartAll()
    {
        foreach (var drone in Drones)
        {
            drone.TurnOnController();
        }
    }

    private void StopAll()
    {
        foreach (var drone in Drones)
        {
            drone.Stop();
        }
    }

    private static IMechanic CreateMechanicWithReflection()
    {
        var mechanicType = Assembly.GetExecutingAssembly()
            .GetTypes()
            .First(type => typeof(IMechanic).IsAssignableFrom(type)
                           && !type.IsInterface
                           && !type.IsAbstract);

        return (IMechanic)Activator.CreateInstance(mechanicType)!;
    }

}
