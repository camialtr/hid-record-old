using System;
using HidRecorder.Models;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace HidRecorder.ViewModels;

public partial class EditorWindowViewModel : ViewModelBase
{
    public string ServerContent { get; private set; } = string.Empty;
    public ObservableCollection<Session> Sessions { get; } = [];
    public ObservableCollection<HidData> HidData { get; } = [];
    
    [RelayCommand]
    private static void Exit() => Environment.Exit(0);
}