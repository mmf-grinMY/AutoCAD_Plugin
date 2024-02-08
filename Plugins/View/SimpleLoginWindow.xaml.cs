using System.Windows;

namespace Plugins.View
{
    /// <summary>
    /// Логика взаимодействия для SimpleLoginWindow.xaml
    /// </summary>
    public partial class SimpleLoginWindow : Window
    {
        public SimpleLoginDto LoginDto { get; private set; }
        public SimpleLoginWindow()
        {
            InitializeComponent();
            var model = new SimpleLoginViewModel();
            DataContext = model;
            model.SaveCommand = new RelayCommand((obj) =>
            {
                DialogResult = true;
                LoginDto = new SimpleLoginDto(model.UserName, passwordBox.Password);
            });
            model.CancelCommand = new RelayCommand((obj) =>
            {
                DialogResult = false;
            });
        }
    }
}
