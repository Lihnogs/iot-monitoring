using IotMonitoringApi.Models;

namespace IotMonitoringApi.Services
{
    public enum AlertType
    {
        None,
        Critical,
        Attention
    }

    public class AlertService
    {
        private const int CriticalConsecutiveCount = 6; // "More than 5" -> 6
        private const int AttentionWindowSize = 50;
        private const decimal Margin = 2m;

        public AlertType CheckAlerts(List<Measurement> history)
        {
            if (history == null || !history.Any())
                return AlertType.None;

            // Sort by date descending to get latest first
            var sortedHistory = history.OrderByDescending(m => m.DataHoraMedicao).ToList();

            // 1. Check Critical Rule (Last 6 measurements)
            if (sortedHistory.Count >= CriticalConsecutiveCount)
            {
                var lastN = sortedHistory.Take(CriticalConsecutiveCount);
                bool isCritical = lastN.All(m => m.Medicao < 1 || m.Medicao > 50);
                
                if (isCritical)
                    return AlertType.Critical;
            }

            // 2. Check Attention Rule (Last 50 measurements Average)
            if (sortedHistory.Count >= AttentionWindowSize)
            {
                var window = sortedHistory.Take(AttentionWindowSize);
                decimal average = window.Average(m => m.Medicao);

                // Margin logic: 
                // Target 1: < 1. Range: (1 - 2) to (1 + 2) -> -1 to 3
                // Target 2: > 50. Range: (50 - 2) to (50 + 2) -> 48 to 52
                
                // Note: The prompt says "margin of error in relation to the limits... constant value 2".
                // "Below 1" -> Limit 1. "Above 50" -> Limit 50.
                // Range 1: 1 +/- 2 => [-1, 3]
                // Range 2: 50 +/- 2 => [48, 52]

                bool inRange1 = average >= -1 && average <= 3;
                bool inRange2 = average >= 48 && average <= 52;

                if (inRange1 || inRange2)
                    return AlertType.Attention;
            }

            return AlertType.None;
        }
    }
}
