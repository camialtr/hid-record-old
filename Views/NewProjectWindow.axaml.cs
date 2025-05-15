using Avalonia.Controls;
using HidRecorder.ViewModels;

namespace HidRecorder.Views;
/// <summary>
/// Window for creating a new project in the HID Recorder application.
/// </summary>
public partial class NewProjectWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the NewProjectWindow class.
    /// </summary>
    public NewProjectWindow()
    {
        InitializeComponent();
        
        // Set the window reference in the ViewModel for dialog handling
        Width = 550;
        Height = 350;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var viewModel = new NewProjectWindowViewModel();
        viewModel.SetParent(this);
        DataContext = viewModel;
    }
}