using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Task3.Models;

namespace Task3.ViewModels;


public partial class MainWindowViewModel : ViewModelBase
{
    private const double DefaultAreaWidth = 780;
    private const double DefaultAreaHeight = 440;
    private const double DroneVisualSize = 20;
    private readonly Random _random = new();
    private readonly DispatcherTimer _timer;
    private readonly List<Quadcopter> _quadcopters = new();
    private int _droneCounter;
    private double _areaWidth = DefaultAreaWidth;
    private double _areaHeight = DefaultAreaHeight;

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

    public void UpdateSimulationAreaSize(double width, double height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        _areaWidth = width;
        _areaHeight = height;

        foreach (var quadcopter in _quadcopters)
        {
            quadcopter.SetAreaSize(_areaWidth, _areaHeight);
        }
    }

    private void AddDrone()
    {
        _droneCounter++;

        var maxInitialX = Math.Max(0, _areaWidth - DroneVisualSize);
        var maxInitialY = Math.Max(0, _areaHeight - DroneVisualSize);

        var quadcopter = new Quadcopter(
            id: _droneCounter,
            initialX: _random.NextDouble() * maxInitialX,
            initialY: _random.NextDouble() * maxInitialY,
            areaWidth: _areaWidth,
            areaHeight: _areaHeight,
            random: _random);

        var @operator = new Operator();
        quadcopter.SubscribeToOperator(@operator);

        var mechanic = CreateMechanicWithReflection();
        var droneVm = new DroneViewModel(quadcopter, @operator, mechanic);

        _quadcopters.Add(quadcopter);
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
