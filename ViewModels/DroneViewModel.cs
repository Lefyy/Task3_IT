using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Task3.Models;

namespace Task3.ViewModels;

public partial class DroneViewModel : ViewModelBase
{
    private readonly Quadcopter _quadcopter;
    private readonly Operator _operator;
    private readonly IMechanic _mechanic;

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private IBrush _brush = Brushes.ForestGreen;

    public DroneViewModel(Quadcopter quadcopter, Operator @operator, IMechanic mechanic)
    {
        _quadcopter = quadcopter;
        _operator = @operator;
        _mechanic = mechanic;

        _quadcopter.GpsDisabled += OnGpsDisabled;
        _quadcopter.EmergencyLandingStarted += OnEmergencyLandingStarted;
        _quadcopter.Landed += OnLanded;

        if (_mechanic is Mechanic concreteMechanic)
        {
            concreteMechanic.RepairMessagePublished += message => Status = message;
        }

        SyncFromModel();
    }

    public string Title => $"Дрон #{_quadcopter.Id}";

    public void TurnOnController()
    {
        Status = $"Оператор включил пульт для дрона #{_quadcopter.Id}.";
        _operator.TurnOnController(_quadcopter);
        SyncFromModel();
    }

    public void SyncFromModel()
    {
        var snapshot = _quadcopter.GetSnapshot();
        X = snapshot.X;
        Y = snapshot.Y;
        Brush = snapshot.GpsEnabled ? Brushes.ForestGreen : Brushes.OrangeRed;
        Status = snapshot.State switch
        {
            DroneState.Flying => $"{Title}: полёт в норме",
            DroneState.Emergency => $"{Title}: авария, GPS отключен",
            DroneState.Landing => $"{Title}: аварийная посадка",
            DroneState.Repairing => $"{Title}: в ремонте",
            _ => $"{Title}: ожидание"
        };
    }

    public void Stop() => _quadcopter.StopSimulation();

    private void OnGpsDisabled(Quadcopter quadcopter)
    {
        Brush = Brushes.OrangeRed;
        Status = $"{Title}: GPS отключился (вероятностное событие).";
    }

    private void OnEmergencyLandingStarted(Quadcopter quadcopter)
    {
        Status = $"{Title}: оператор инициировал аварийную посадку.";
    }

    private async void OnLanded(Quadcopter quadcopter)
    {
        Status = $"{Title}: сел, ожидает механика.";
        await _mechanic.RepairAsync(_quadcopter, CancellationToken.None);
        Status = $"{Title}: ремонт завершен, повторный взлёт.";
        _operator.TurnOnController(_quadcopter);
    }
}
