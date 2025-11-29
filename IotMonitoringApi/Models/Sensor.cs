using System.ComponentModel.DataAnnotations;

namespace IotMonitoringApi.Models
{
    public class Sensor
    {
        [Key]
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        
        public int? EquipmentId { get; set; }
        public Equipment? Equipment { get; set; }
    }
}
