using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HidRecorder.Models;

public sealed class Session(string platform, string file, bool export = true) : INotifyPropertyChanged
{
    private string _platform = platform;
    private string _name = System.IO.Path.GetFileNameWithoutExtension(file);
    private bool _export = export;

    public string Platform
    {
        get => _platform;
        set
        {
            if (_platform == value) return;
            _platform = value;
            OnPropertyChanged();
        }
    }
    
    public string Name
    {
        get => _name;
        set
        {
            if (_name == value) return;
            _name = value;
            OnPropertyChanged();
        }
    }
    
    public bool Export
    {
        get => _export;
        set
        {
            if (_export == value) return;
            _export = value;
            OnPropertyChanged();
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}