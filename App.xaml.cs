using System.Windows;
using rec_tool.Models;

namespace rec_tool;

public partial class App : Application
{
    public static readonly Settings Settings = new() { CurrentIp = "192.168.1.3" };
}
