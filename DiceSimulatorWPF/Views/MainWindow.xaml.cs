using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using LiveCharts;

namespace DiceSimulatorWPF
{
    public partial class MainWindow : Window
    {
        private bool isChartView = false;
        public ChartValues<double> SimulationValues { get; set; }
        public ChartValues<double> TheoryValues { get; set; }

        private Dictionary<int, int> lastSimulationFrequency = null;
        private double lastEmpiricalMean = 0;
        private TheoreticalResult lastTheoreticalResult = null;

        public List<string> SimulationLabels { get; set; } = new List<string>();
        public List<string> TheoryLabels { get; set; } = new List<string>();
        public Func<double, string> ProbabilityFormatter { get; set; } = value => $"{value:F4}";
        public static MainWindow Instance { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
            DataContext = this;
            SimulationValues = new ChartValues<double>();
            TheoryValues = new ChartValues<double>();
        }

        private async void RollDice_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs(out string errorMessage))
            {
                MessageBox.Show(
                    errorMessage
                        + "\nПример: '1d6 2d8' для одного 6-гранного и двух 8-гранных кубиков.",
                    "Ошибка ввода",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            var diceList = ParseDiceInput(DiceInput.Text);
            int numThrows = int.Parse(RollsInput.Text);

            RollDiceButton.IsEnabled = false;
            RollDiceButton.Content = "Обработка...";

            var simulationResult = await Task.Run(() => SimulateDiceRolls(diceList, numThrows));
            var theoreticalResult = CalculateTheoreticalValues(diceList);

            lastSimulationFrequency = simulationResult.frequency;
            lastEmpiricalMean = simulationResult.empiricalValue;
            lastTheoreticalResult = theoreticalResult;

            UpdateUI();

            RollDiceButton.IsEnabled = true;
            RollDiceButton.Content = "Бросить дайсы";
        }

        private (Dictionary<int, int> frequency, double empiricalValue) SimulateDiceRolls(
            List<(int count, int edges)> diceList,
            int numThrows
        )
        {
            var frequency = new Dictionary<int, int>();
            var rng = new Random();
            long totalSum = 0;

            for (int i = 0; i < numThrows; i++)
            {
                int sum = RollDice(diceList, rng);
                frequency[sum] = frequency.GetValueOrDefault(sum) + 1;
                totalSum += sum;
            }
            return (frequency, (double)totalSum / numThrows);
        }

        private TheoreticalResult CalculateTheoreticalValues(List<(int count, int edges)> diceList)
        {
            double value = diceList.Sum(d => (d.edges + 1) / 2.0 * d.count);
            double variance = diceList.Sum(d => ((Math.Pow(d.edges, 2) - 1) / 12.0) * d.count);
            var probabilities = CalculateTheoreticalProbabilities(diceList);
            return new TheoreticalResult(value, variance, Math.Sqrt(variance), probabilities);
        }

        private void UpdateUI()
        {
            if (lastSimulationFrequency == null || lastTheoreticalResult == null)
            {
                if (!isChartView)
                {
                    ResultsSimulationText.AppendText(
                        "Нажмите 'Бросить дайсы' для начала симуляции."
                    );
                    ResultsTheoryText.AppendText("");
                }
                SimulationValues.Clear();
                TheoryValues.Clear();
                SimulationLabels.Clear();
                TheoryLabels.Clear();
                return;
            }

            int numThrows = int.TryParse(RollsInput.Text, out int n) ? n : 1;
            if (isChartView)
            {
                SimulationValues.Clear();
                SimulationLabels.Clear();
                var simOrdered = lastSimulationFrequency.OrderBy(p => p.Key).ToList();
                foreach (var pair in simOrdered)
                {
                    SimulationValues.Add(pair.Value);
                    SimulationLabels.Add(pair.Key.ToString());
                }
                SimulationChart.Series[0].Values = SimulationValues;

                TheoryValues.Clear();
                TheoryLabels.Clear();
                var theoOrdered = lastTheoreticalResult.Probabilities.OrderBy(p => p.Key).ToList();
                foreach (var pair in theoOrdered)
                {
                    TheoryValues.Add(pair.Value);
                    TheoryLabels.Add(pair.Key.ToString());
                }

                TheoryChart.Series[0].Values = TheoryValues;
            }
            else
            {
                BuildSimulationTextRich(lastSimulationFrequency, lastEmpiricalMean, numThrows);
                BuildTheoryTextRich(lastTheoreticalResult);
            }
        }

        private void BuildSimulationTextRich(
            Dictionary<int, int> frequency,
            double empiricalMean,
            int numThrows
        )
        {
            var doc = new FlowDocument();
            var title = new Paragraph(
                new Run("🎲 Симуляция")
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.DarkGreen
                }
            );
            doc.Blocks.Add(title);
            doc.Blocks.Add(new Paragraph(new Run($"Количество бросков: {numThrows}")));
            doc.Blocks.Add(
                new Paragraph(new Run(new string('─', 30)) { Foreground = Brushes.Gray })
            );
            doc.Blocks.Add(
                new Paragraph(
                    new Run($"Средняя сумма: {empiricalMean:F4}") { Foreground = Brushes.Blue }
                )
            );
            doc.Blocks.Add(new Paragraph(new Run("Частоты:")));

            foreach (var pair in frequency.OrderBy(p => p.Key))
            {
                double probability = pair.Value / (double)numThrows;
                var line = new Paragraph();
                line.Inlines.Add(
                    new Run($"  Сумма {pair.Key, -3}: ") { Foreground = Brushes.Black }
                );
                line.Inlines.Add(
                    new Run($"{pair.Value, 5} раз ") { Foreground = Brushes.DarkGreen }
                );
                line.Inlines.Add(new Run($"({probability:F4})") { Foreground = Brushes.DarkBlue });
                doc.Blocks.Add(line);
            }
            doc.Blocks.Add(
                new Paragraph(new Run(new string('─', 30)) { Foreground = Brushes.Gray })
            );
            ResultsSimulationText.Document = doc;
        }

        private void BuildTheoryTextRich(TheoreticalResult theo)
        {
            var doc = new FlowDocument();
            var title = new Paragraph(
                new Run("📌 Теория")
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.DarkBlue
                }
            );
            doc.Blocks.Add(title);
            doc.Blocks.Add(
                new Paragraph(new Run(new string('─', 30)) { Foreground = Brushes.Gray })
            );
            doc.Blocks.Add(
                new Paragraph(
                    new Run($"Мат. ожидание (M): {theo.Value:F4}") { Foreground = Brushes.Blue }
                )
            );
            doc.Blocks.Add(
                new Paragraph(
                    new Run($"Дисперсия (D): {theo.Variance:F4}") { Foreground = Brushes.Blue }
                )
            );
            doc.Blocks.Add(
                new Paragraph(
                    new Run($"Отклонение (σ): {theo.StdDev:F4}") { Foreground = Brushes.Blue }
                )
            );
            doc.Blocks.Add(
                new Paragraph(
                    new Run($"Сумма вероятностей: {theo.Probabilities.Values.Sum():F4}")
                    {
                        Foreground = Brushes.Blue
                    }
                )
            );
            doc.Blocks.Add(
                new Paragraph(new Run(new string('─', 30)) { Foreground = Brushes.Gray })
            );
            doc.Blocks.Add(new Paragraph(new Run("Вероятности:")));

            foreach (var pair in theo.Probabilities.OrderBy(p => p.Key))
            {
                var line = new Paragraph();
                line.Inlines.Add(
                    new Run($"  Сумма {pair.Key, -3}: ") { Foreground = Brushes.Black }
                );
                line.Inlines.Add(new Run($"{pair.Value:F8}") { Foreground = Brushes.DarkBlue });
                doc.Blocks.Add(line);
            }
            doc.Blocks.Add(
                new Paragraph(new Run(new string('─', 30)) { Foreground = Brushes.Gray })
            );
            ResultsTheoryText.Document = doc;
        }

        private List<(int count, int edges)> ParseDiceInput(string input)
        {
            var diceList = new List<(int count, int edges)>();
            foreach (Match match in Regex.Matches(input, @"(\d*)d(\d+)"))
            {
                int count = string.IsNullOrEmpty(match.Groups[1].Value)
                    ? 1
                    : int.Parse(match.Groups[1].Value);
                int edges = int.Parse(match.Groups[2].Value);
                diceList.Add((count, edges));
            }
            return diceList;
        }

        private int RollDice(List<(int count, int edges)> diceList, Random rng)
        {
            int sum = 0;
            foreach (var (count, edges) in diceList)
            {
                for (int i = 0; i < count; i++)
                {
                    sum += rng.Next(1, edges + 1);
                }
            }
            return sum;
        }

        private Dictionary<int, double> CalculateTheoreticalProbabilities(
            List<(int count, int edges)> diceList
        )
        {
            Dictionary<int, double> probabilities = new Dictionary<int, double> { { 0, 1.0 } };

            foreach (var (count, edges) in diceList)
            {
                var singleDieProbs = new Dictionary<int, double>();
                double prob = 1.0 / edges;
                for (int roll = 1; roll <= edges; roll++)
                {
                    singleDieProbs[roll] = prob;
                }

                for (int i = 0; i < count; i++)
                {
                    var newProbs = new Dictionary<int, double>();
                    foreach (var (prevSum, prevProb) in probabilities)
                    {
                        foreach (var (roll, rollProb) in singleDieProbs)
                        {
                            int newSum = prevSum + roll;
                            newProbs[newSum] =
                                newProbs.GetValueOrDefault(newSum) + prevProb * rollProb;
                        }
                    }
                    probabilities = newProbs;
                }
            }

            return probabilities;
        }

        private void ToggleView_Click(object sender, RoutedEventArgs e)
        {
            isChartView = !isChartView;
            ResultsSimulationTextView.Visibility = isChartView
                ? Visibility.Collapsed
                : Visibility.Visible;
            SimulationChart.Visibility = isChartView ? Visibility.Visible : Visibility.Collapsed;
            ResultsTheoryTextView.Visibility = isChartView
                ? Visibility.Collapsed
                : Visibility.Visible;
            TheoryChart.Visibility = isChartView ? Visibility.Visible : Visibility.Collapsed;

            UpdateUI();
        }

        private bool ValidateInputs(out string errorMessage)
        {
            errorMessage = string.Empty;
            if (!int.TryParse(RollsInput.Text, out int numThrows) || numThrows <= 0)
            {
                errorMessage = "Введите корректное положительное количество бросков!";
                return false;
            }

            var diceList = ParseDiceInput(DiceInput.Text);
            if (diceList.Count == 0)
            {
                errorMessage = "Введите корректные кубики в формате '1d6 2d8 1d4'!";
                return false;
            }

            foreach (var (count, edges) in diceList)
            {
                if (count <= 0 || edges <= 0)
                {
                    errorMessage = "Количество кубиков и граней должны быть положительными!";
                    return false;
                }
            }

            return true;
        }
    }
}
