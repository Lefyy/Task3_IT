using Avalonia;
using Avalonia.Controls;
using Task3.ViewModels;

namespace Task3.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void SimulationCanvas_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.UpdateSimulationAreaSize(e.NewSize.Width, e.NewSize.Height);
        }
    }

}