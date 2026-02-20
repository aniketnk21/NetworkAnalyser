using System.Windows;
using NetworkAnalyser.Desktop.ViewModels;

namespace NetworkAnalyser.Desktop;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Shutdown();
        base.OnClosed(e);
    }
}