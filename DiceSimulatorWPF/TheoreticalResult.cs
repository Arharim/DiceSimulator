namespace DiceSimulatorWPF
{
    class TheoreticalResult
    {
        public double Value { get; }
        public double Variance { get; }
        public double StdDev { get; }
        public Dictionary<int, double> Probabilities { get; }

        public TheoreticalResult(
            double value,
            double variance,
            double stdDev,
            Dictionary<int, double> probs
        )
        {
            Value = value;
            Variance = variance;
            StdDev = stdDev;
            Probabilities = probs;
        }
    }
}
