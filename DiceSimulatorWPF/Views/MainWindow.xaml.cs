using DiceSimulatorWPF.ViewModels;
using System.Windows;

namespace DiceSimulatorWPF.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
