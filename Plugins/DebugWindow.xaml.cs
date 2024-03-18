using Autodesk.AutoCAD.GraphicsSystem;
using System.Windows;

namespace Plugins
{
    /// <summary>
    /// Логика взаимодействия для DebugWindow.xaml
    /// </summary>
    public partial class DebugWindow : Window
    {
        public DebugViewModel ViewModel { get; }
        public DebugWindow()
        {
            InitializeComponent();
            ViewModel = new DebugViewModel(Dispatcher);
            DataContext = ViewModel;
        }
    }
}
