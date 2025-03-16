using DiceSimulatorWPF.Models;
using DiceSimulatorWPF.Utility;
using LiveCharts;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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

        private RichTextContent _simulationContent = new RichTextContent();
        private RichTextContent _theoryContent = new RichTextContent();

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
                UpdateDisplay();
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

        public RichTextContent SimulationContent
        {
            get => _simulationContent;
            set
            {
                _simulationContent = value;
                OnPropertyChanged();
            }
        }

        public RichTextContent TheoryContent
        {
            get => _theoryContent;
            set
            {
                _theoryContent = value;
                OnPropertyChanged();
            }
        }

        public ICommand RollDiceCommand { get; }
        public ICommand ToggleViewCommand { get; }

        public MainViewModel()
        {
            RollDiceCommand = new RelayCommand(
                async () => await RollDiceAsync(),
                () => !IsProcessing
            );
            ToggleViewCommand = new RelayCommand(() => IsChartView = !IsChartView);
            UpdateDisplay();
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

            UpdateDisplay();
            IsProcessing = false;
        }

        private void UpdateDisplay()
        {
            if (_lastSimulationFrequency == null || _lastTheoreticalResult == null)
            {
                if (!IsChartView)
                {
                    SimulationContent = new RichTextContent
                    {
                        Sections =
                        {
                            new RichTextSection
                            {
                                Text = "Нажмите 'Бросить дайсы' для начала симуляции.",
                                Color = "#808080"
                            }
                        }
                    };
                    TheoryContent = new RichTextContent();
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
                SimulationContent = BuildSimulationRichText(
                    _lastSimulationFrequency,
                    _lastEmpiricalValue,
                    int.Parse(RollsInput)
                );
                TheoryContent = BuildTheoryRichText(_lastTheoreticalResult);
            }

            OnPropertyChanged(nameof(SimulationContent));
            OnPropertyChanged(nameof(TheoryContent));
            OnPropertyChanged(nameof(SimulationValues));
            OnPropertyChanged(nameof(TheoryValues));
            OnPropertyChanged(nameof(SimulationLabels));
            OnPropertyChanged(nameof(TheoryLabels));
        }

        private RichTextContent BuildSimulationRichText(
            Dictionary<int, int> frequency,
            double empiricalValue,
            int numThrows
        )
        {
            var content = new RichTextContent();
            content.Sections.Add(
                new RichTextSection
                {
                    Text = $"🎲 Симуляция {numThrows} бросков",
                    IsBold = true,
                    Color = "#006400",
                    IsNewParagraph = true
                }
            );
            content.Sections.Add(
                new RichTextSection
                {
                    Text = $"Средняя сумма: {empiricalValue:F4}",
                    Color = "#0000FF",
                    IsNewParagraph = true
                }
            );
            content.Sections.Add(
                new RichTextSection
                {
                    Text = "Частоты:\n",
                    IsBold = true,
                    IsNewParagraph = true
                }
            );

            foreach (var pair in frequency.OrderBy(p => p.Key))
            {
                double probability = pair.Value / (double)numThrows;
                content.Sections.Add(
                    new RichTextSection
                    {
                        Text = $"  Сумма {pair.Key, -3}: ",
                        Color = "#000000",
                        IsNewParagraph = true
                    }
                );
                content.Sections.Add(
                    new RichTextSection
                    {
                        Text = $"{pair.Value, 5} раз ",
                        Color = "#008000",
                        IsBold = true,
                        IsNewParagraph = false
                    }
                );
                content.Sections.Add(
                    new RichTextSection
                    {
                        Text = $"({probability:F4})",
                        Color = "#00008B",
                        IsNewParagraph = false
                    }
                );
            }
            return content;
        }

        private RichTextContent BuildTheoryRichText(TheoreticalResult theo)
        {
            var content = new RichTextContent();
            content.Sections.Add(
                new RichTextSection
                {
                    Text = "📌 Теоретические расчёты",
                    IsBold = true,
                    Color = "#00008B",
                    IsNewParagraph = true
                }
            );
            content.Sections.Add(
                new RichTextSection
                {
                    Text = $"  Мат. ожидание (M): {theo.Value:F4}",
                    Color = "#0000FF",
                    IsNewParagraph = true
                }
            );
            content.Sections.Add(
                new RichTextSection
                {
                    Text = $"  Дисперсия (D): {theo.Variance:F4}",
                    Color = "#0000FF",
                    IsNewParagraph = true
                }
            );
            content.Sections.Add(
                new RichTextSection
                {
                    Text = $"  Отклонение (σ): {theo.StdDev:F4}",
                    Color = "#0000FF",
                    IsNewParagraph = true
                }
            );
            content.Sections.Add(
                new RichTextSection
                {
                    Text = $"  Сумма вероятностей: {theo.Probabilities.Values.Sum():F4}",
                    Color = "#0000FF",
                    IsNewParagraph = true
                }
            );
            content.Sections.Add(new RichTextSection { Text = "Вероятности:", IsBold = true });

            foreach (var pair in theo.Probabilities.OrderBy(p => p.Key))
            {
                content.Sections.Add(
                    new RichTextSection
                    {
                        Text = $"  Сумма {pair.Key, -3}: ",
                        Color = "#000000",
                        IsNewParagraph = true
                    }
                );
                content.Sections.Add(
                    new RichTextSection
                    {
                        Text = $"{pair.Value:F8}",
                        Color = "#00008B",
                        IsBold = true,
                        IsNewParagraph = false
                    }
                );
            }
            return content;
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
