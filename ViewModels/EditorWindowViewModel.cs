using System;
using Avalonia.Controls;
using HidRecorder.Views;
using HidRecorder.Models;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace HidRecorder.ViewModels;

public partial class EditorWindowViewModel : ViewModelBase
{
    public string ServerContent { get; private set; } = string.Empty;
    public ObservableCollection<Session> Sessions { get; } = [];
    public ObservableCollection<HidData> HidData { get; } = [];
    
    private Window? _parentWindow;
    
    public void SetParentWindow(Window window)
    {
        _parentWindow = window;
    }

    [RelayCommand]
    private void New()
    {
        var newProjectWindow = new NewProjectWindow();
        newProjectWindow.Show();
        
        _parentWindow?.Close();
    }
    
    [RelayCommand]
    private static void Exit() => Environment.Exit(0);
}