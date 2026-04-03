using ElevatorMaintenanceSystem.ViewModels;

namespace ElevatorMaintenanceSystem.Views;

public partial class MainWindow
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
