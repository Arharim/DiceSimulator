using System.Text.RegularExpressions;
using DiceSimulatorWPF.Utility;

namespace DiceSimulatorWPF.Models
{
    public class DiceModel
    {
        public List<(int count, int edges)> ParseDiceInput(string input)
        {
            var diceList = new List<(int count, int edges)>();
            foreach (Match match in Regex.Matches(input, @"(\d*)d(\d+)"))
            {
                int count = string.IsNullOrEmpty(match.Groups[1].Value)
                    ? 1
                    : int.Parse(match.Groups[1].Value);
                int edges = int.Parse(match.Groups[2].Value);
                if (count > 0 && edges > 0)
                {
                    diceList.Add((count, edges));
                }
            }
            return diceList;
        }

        public (Dictionary<int, int> frequency, double empiricalValue) SimulateDiceRolls(
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

        public TheoreticalResult CalculateTheoreticalValues(List<(int count, int edges)> diceList)
        {
            double value = diceList.Sum(d => (d.edges + 1) / 2.0 * d.count);
            double variance = diceList.Sum(d => ((Math.Pow(d.edges, 2) - 1) / 12.0) * d.count);
            var probabilities = CalculateTheoreticalProbabilities(diceList);
            return new TheoreticalResult(value, variance, Math.Sqrt(variance), probabilities);
        }

        public Dictionary<int, double> CalculateTheoreticalProbabilities(
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
    }
}
