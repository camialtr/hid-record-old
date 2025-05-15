using Avalonia.Controls;
using HidRecorder.ViewModels;

namespace HidRecorder.Views;

/// <summary>
/// Main editor window for the HID Recorder application.
/// </summary>
public partial class EditorWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the EditorWindow class.
    /// </summary>
    public EditorWindow()
    {
        InitializeComponent();
        
        // Set the parent window reference in the ViewModel for navigation
        var viewModel = new EditorWindowViewModel();
        viewModel.SetParentWindow(this);
        DataContext = viewModel;
    }
}