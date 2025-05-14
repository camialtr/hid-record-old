using Avalonia.Controls;
using HidRecorder.ViewModels;

namespace HidRecorder.Views;
public partial class NewProjectWindow : Window
{
    public NewProjectWindow()
    {
        InitializeComponent();
        
        Width = 550;
        Height = 350;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var viewModel = new NewProjectWindowViewModel();
        viewModel.SetParent(this);
        DataContext = viewModel;
    }
}