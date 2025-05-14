using Avalonia.Controls;
using HidRecorder.ViewModels;

namespace HidRecorder.Views;

public partial class EditorWindow : Window
{
    public EditorWindow()
    {
        InitializeComponent();
        
        var viewModel = new EditorWindowViewModel();
        viewModel.SetParentWindow(this);
        DataContext = viewModel;
    }
}