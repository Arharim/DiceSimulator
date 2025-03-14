using DiceSimulatorWPF.Models;
using DiceSimulatorWPF.Utility;
using LiveCharts;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace DiceSimulatorWPF.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DiceModel _diceModel = new DiceModel();
        private string _diceInput = "";
        private string _rollsInput = "100";
        private bool _isChartView = false;
        private bool _isProcessing = false;
        private Dictionary<int, int> _lastSimulationFrequency;
        private double _lastEmpiricalValue;
        private TheoreticalResult _lastTheoreticalResult;

        public event PropertyChangedEventHandler PropertyChanged;

        public string DiceInput
        {
            get => _diceInput;
            set
            {
                _diceInput = value;
                OnPropertyChanged();
            }
        }

        public string RollsInput
        {
            get => _rollsInput;
            set
            {
                _rollsInput = value;
                OnPropertyChanged();
            }
        }
        public bool IsChartView
        {
            get => _isChartView;
            set
            {
                _isChartView = value;
                UpdateDispaly();
                OnPropertyChanged();
            }
        }
        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                _isProcessing = value;
                OnPropertyChanged();
            }
        }

        public ChartValues<double> SimulationValues { get; } = new ChartValues<double>();
        public ChartValues<double> TheoryValues { get; } = new ChartValues<double>();
        public List<string> SimulationLabels { get; set; } = new List<string>();
        public List<string> TheoryLabels { get; set; } = new List<string>();
        public string SimulationText { get; private set; } = "Нажмите 'Бросить дайсы' для начала симуляции.";
        public string TheoryText { get; private set; } = "";

        public ICommand RollDiceCommand { get; }
        public ICommand ToggleViewCommand { get; }

        public MainViewModel()
        {
            RollDiceCommand = new RelayCommand(
                async () => await RollDiceAsync(),
                () => !IsProcessing
            );
            ToggleViewCommand = new RelayCommand(() => IsChartView = !IsChartView);
        }

        private async Task RollDiceAsync()
        {
            if (!ValidateInputs(out string errorMessage))
            {
                MessageBox.Show(errorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            IsProcessing = true;
            var diceList = _diceModel.ParseDiceInput(DiceInput);
            int numThrows = int.Parse(RollsInput);

            var (frequency, empiricalValue) = await Task.Run(
                () => _diceModel.SimulateDiceRolls(diceList, numThrows)
            );
            var theroeticalResult = _diceModel.CalculateTheoreticalValues(diceList);

            _lastSimulationFrequency = frequency;
            _lastEmpiricalValue = empiricalValue;
            _lastTheoreticalResult = theroeticalResult;

            UpdateDispaly();
            IsProcessing = false;
        }

        private void UpdateDispaly()
        {
            if (_lastSimulationFrequency == null || _lastTheoreticalResult == null)
            {
                if (!IsChartView)
                {
                    SimulationText = "Нажмите 'Бросить дайсы' для начала симуляции.";
                    TheoryText = "";
                }
                SimulationValues.Clear();
                TheoryValues.Clear();
                SimulationLabels.Clear();
                TheoryLabels.Clear();
            }
            else if (IsChartView)
            {
                SimulationValues.Clear();
                SimulationLabels.Clear();
                var simOredered = _lastSimulationFrequency.OrderBy(p => p.Key).ToList();
                foreach (var pair in simOredered)
                {
                    SimulationValues.Add(pair.Value);
                    SimulationLabels.Add(pair.Key.ToString());
                }

                TheoryValues.Clear();
                TheoryLabels.Clear();
                var theoOrdered = _lastTheoreticalResult.Probabilities.OrderBy(p => p.Key).ToList();
                foreach (var pair in theoOrdered)
                {
                    TheoryValues.Add(pair.Value);
                    TheoryLabels.Add(pair.Key.ToString());
                }
            }
            else
            {
                SimulationText = BuildSimulationText(_lastSimulationFrequency, _lastEmpiricalValue, int.Parse(RollsInput));
                TheoryText = BuildTheoryText(_lastTheoreticalResult);
            }

            OnPropertyChanged(nameof(SimulationText));
            OnPropertyChanged(nameof(TheoryText));
            OnPropertyChanged(nameof(SimulationValues));
            OnPropertyChanged(nameof(TheoryValues));
            OnPropertyChanged(nameof(SimulationLabels));
            OnPropertyChanged(nameof(TheoryLabels));
        }

        private string BuildSimulationText(Dictionary<int, int> frequency, double empiricalValue, int numThrows)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"🎲 Симуляция {numThrows} бросков");
            sb.AppendLine($"Средняя сумма: {empiricalValue:F4}");
            foreach (var pair in frequency.OrderBy(p => p.Key))
                sb.AppendLine($"  Сумма {pair.Key}: {pair.Value} раз ({pair.Value / (double)numThrows:F4})");
            return sb.ToString();
        }

        private string BuildTheoryText(TheoreticalResult theo)
        {
            var sb = new StringBuilder();
            sb.AppendLine("📌 Теоретические расчёты");
            sb.AppendLine($"  Мат. ожидание (M): {theo.Value:F4}");
            sb.AppendLine($"  Дисперсия (D): {theo.Variance:F4}");
            sb.AppendLine($"  Отклонение (σ): {theo.StdDev:F4}");
            sb.AppendLine($"  Сумма вероятностей: {theo.Probabilities.Values.Sum():F4}");
            sb.AppendLine("Вероятности:");
            foreach (var pair in theo.Probabilities.OrderBy(p => p.Key))
                sb.AppendLine($"  Сумма {pair.Key}: {pair.Value:F8}");
            return sb.ToString();
        }

        private bool ValidateInputs(out string errorMessage)
        {
            errorMessage = "";
            if (!int.TryParse(RollsInput, out int numThrows) || numThrows <= 0)
            {
                errorMessage = "Введите корректное положительное количество бросков!";
                return false;
            }

            var diceList = _diceModel.ParseDiceInput(DiceInput);
            if (diceList.Count == 0)
            {
                errorMessage = "Введите корректные кубики в формате '1d6 2d8 1d4'!";
                return false;
            }
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

        public void Execute(object parameter) => _execute();
    }
}
