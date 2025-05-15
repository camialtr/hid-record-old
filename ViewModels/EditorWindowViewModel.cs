using System;
using Avalonia.Controls;
using HidRecorder.Views;
using HidRecorder.Models;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace HidRecorder.ViewModels;

/// <summary>
/// ViewModel for the main editor window of the application.
/// </summary>
public partial class EditorWindowViewModel : ViewModelBase
{
    /// <summary>
    /// Gets the current server status content to display.
    /// </summary>
    public string ServerContent { get; private set; } = string.Empty;
    
    /// <summary>
    /// Collection of recording sessions.
    /// </summary>
    public ObservableCollection<Session> Sessions { get; } = [];
    
    /// <summary>
    /// Collection of HID data records to display.
    /// </summary>
    public ObservableCollection<HidData> HidData { get; } = [];
    
    /// <summary>
    /// Reference to the parent window.
    /// </summary>
    private Window? _parentWindow;
    
    /// <summary>
    /// Sets the parent window reference for navigation and window management.
    /// </summary>
    /// <param name="window">The parent window instance.</param>
    public void SetParentWindow(Window window)
    {
        _parentWindow = window;
    }

    /// <summary>
    /// Creates a new project by opening the New Project window.
    /// </summary>
    [RelayCommand]
    private void New()
    {
        var newProjectWindow = new NewProjectWindow();
        newProjectWindow.Show();
        
        _parentWindow?.Close();
    }
    
    /// <summary>
    /// Exits the application.
    /// </summary>
    [RelayCommand]
    private static void Exit() => Environment.Exit(0);
}