using Avalonia.Controls;
using HidRecorder.ViewModels;

namespace HidRecorder.Views;

public partial class EditorWindow : Window
{
    public EditorWindow()
    {
        InitializeComponent();

        DataContext = new EditorWindowViewModel();
    }
}