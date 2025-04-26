using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace HidRecorder.Windows;

public partial class EditorWindow : Window
{
    public EditorWindow()
    {
        InitializeComponent();
    }

    private void ExitMenuItem_OnClick(object? sender, RoutedEventArgs e) => Environment.Exit(0);
}