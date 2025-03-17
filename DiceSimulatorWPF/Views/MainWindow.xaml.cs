using System.Windows;
using DiceSimulatorWPF.ViewModels;

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
