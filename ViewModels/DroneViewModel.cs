using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Task3.Models;

namespace Task3.ViewModels;

public partial class DroneViewModel : ViewModelBase
{
    private static readonly TimeSpan EventDisplayDuration = TimeSpan.FromSeconds(2);
    private readonly Quadcopter _quadcopter;
    private readonly Operator _operator;
    private readonly IMechanic _mechanic;
    private DateTime _lastEventAt = DateTime.MinValue;

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private string _stateText = string.Empty;

    [ObservableProperty]
    private string _lastEventText = string.Empty;

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
            concreteMechanic.RepairMessagePublished += message => SetEventText(message);
        }

        SyncFromModel();
    }

    public string Title => $"Дрон #{_quadcopter.Id}";

    public void Start()
    {
        TurnOnController();
    }

    public void TurnOnController()
    {
        SetEventText($"Оператор включил пульт для дрона #{_quadcopter.Id}.");
        _operator.TurnOnController(_quadcopter);
        SyncFromModel();
    }

    public void SyncFromModel()
    {
        var snapshot = _quadcopter.GetSnapshot();
        X = snapshot.X;
        Y = snapshot.Y;
        Brush = snapshot.GpsEnabled ? Brushes.ForestGreen : Brushes.OrangeRed;
        StateText = snapshot.State switch
        {
            DroneState.Flying => $"{Title}: полёт в норме",
            DroneState.Emergency => $"{Title}: авария, GPS отключен",
            DroneState.Landing => $"{Title}: аварийная посадка",
            DroneState.Repairing => $"{Title}: в ремонте",
            _ => $"{Title}: ожидание"
        };

        var eventIsFresh = !string.IsNullOrWhiteSpace(LastEventText)
                           && DateTime.UtcNow - _lastEventAt <= EventDisplayDuration;

        Status = eventIsFresh ? LastEventText : StateText;
    }

    public void Stop()
    {
        _quadcopter.StopSimulation();
        SyncFromModel();
    }


    private void OnGpsDisabled(Quadcopter quadcopter)
    {
        Brush = Brushes.OrangeRed;
        SetEventText($"{Title}: GPS отключился (вероятностное событие).");
    }

    private void OnEmergencyLandingStarted(Quadcopter quadcopter)
    {
        SetEventText($"{Title}: оператор инициировал аварийную посадку.");
    }

    private async void OnLanded(Quadcopter quadcopter)
    {
        SetEventText($"{Title}: сел, ожидает механика.");
        await _mechanic.RepairAsync(_quadcopter, CancellationToken.None);
        SetEventText($"{Title}: ремонт завершен, запускаем повторно.");
        Start();
    }

    private void SetEventText(string text)
    {
        LastEventText = text;
        _lastEventAt = DateTime.UtcNow;
        Status = text;
    }
}
