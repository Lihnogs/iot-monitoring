using System;
using System.ComponentModel.DataAnnotations;

namespace IotMonitoringApi.Models
{
    public class Measurement
    {
        [Key]
        public int Id { get; set; }
        
        public string Codigo { get; set; } = string.Empty;
        
        public DateTimeOffset DataHoraMedicao { get; set; }
        
        public decimal Medicao { get; set; }
    }
}
