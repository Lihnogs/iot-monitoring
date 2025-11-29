using IotMonitoringApi.Models;
using IotMonitoringApi.Services;
using Xunit;

namespace IotMonitoringApi.Tests
{
    public class AlertServiceTests
    {
        private readonly AlertService _service;

        public AlertServiceTests()
        {
            _service = new AlertService();
        }

        [Fact]
        public void CheckAlerts_ShouldReturnCritical_WhenLast6MeasurementsAreBelow1()
        {
            // Arrange
            var history = new List<Measurement>();
            for (int i = 0; i < 6; i++)
            {
                history.Add(new Measurement { Medicao = 0.5m, DataHoraMedicao = DateTimeOffset.Now.AddMinutes(-i) });
            }

            // Act
            var result = _service.CheckAlerts(history);

            // Assert
            Assert.Equal(AlertType.Critical, result);
        }

        [Fact]
        public void CheckAlerts_ShouldReturnCritical_WhenLast6MeasurementsAreAbove50()
        {
            // Arrange
            var history = new List<Measurement>();
            for (int i = 0; i < 6; i++)
            {
                history.Add(new Measurement { Medicao = 55m, DataHoraMedicao = DateTimeOffset.Now.AddMinutes(-i) });
            }

            // Act
            var result = _service.CheckAlerts(history);

            // Assert
            Assert.Equal(AlertType.Critical, result);
        }

        [Fact]
        public void CheckAlerts_ShouldReturnNone_WhenOnly5MeasurementsAreCritical()
        {
            // Arrange
            var history = new List<Measurement>();
            for (int i = 0; i < 5; i++)
            {
                history.Add(new Measurement { Medicao = 0.5m, DataHoraMedicao = DateTimeOffset.Now.AddMinutes(-i) });
            }
            // Add one normal
            history.Add(new Measurement { Medicao = 25m, DataHoraMedicao = DateTimeOffset.Now.AddMinutes(-10) });

            // Act
            var result = _service.CheckAlerts(history);

            // Assert
            Assert.Equal(AlertType.None, result);
        }

        [Fact]
        public void CheckAlerts_ShouldReturnAttention_WhenAverageIsWithinLowMargin()
        {
            // Arrange: Average around 0 (between -1 and 3)
            var history = new List<Measurement>();
            for (int i = 0; i < 50; i++)
            {
                history.Add(new Measurement { Medicao = 0m, DataHoraMedicao = DateTimeOffset.Now.AddMinutes(-i) });
            }

            // Act
            var result = _service.CheckAlerts(history);

            // Assert
            Assert.Equal(AlertType.Attention, result);
        }

        [Fact]
        public void CheckAlerts_ShouldReturnAttention_WhenAverageIsWithinHighMargin()
        {
            // Arrange: Average around 50 (between 48 and 52)
            var history = new List<Measurement>();
            for (int i = 0; i < 50; i++)
            {
                history.Add(new Measurement { Medicao = 50m, DataHoraMedicao = DateTimeOffset.Now.AddMinutes(-i) });
            }

            // Act
            var result = _service.CheckAlerts(history);

            // Assert
            Assert.Equal(AlertType.Attention, result);
        }

        [Fact]
        public void CheckAlerts_ShouldReturnNone_WhenAverageIsNormal()
        {
            // Arrange: Average 25
            var history = new List<Measurement>();
            for (int i = 0; i < 50; i++)
            {
                history.Add(new Measurement { Medicao = 25m, DataHoraMedicao = DateTimeOffset.Now.AddMinutes(-i) });
            }

            // Act
            var result = _service.CheckAlerts(history);

            // Assert
            Assert.Equal(AlertType.None, result);
        }
    }
}
