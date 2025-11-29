using System.ComponentModel.DataAnnotations;

namespace IotMonitoringApi.Models
{
    public class Equipment
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
    }
}
