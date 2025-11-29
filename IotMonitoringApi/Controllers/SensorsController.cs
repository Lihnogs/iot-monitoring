using Microsoft.AspNetCore.Mvc;
using IotMonitoringApi.Data;
using IotMonitoringApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IotMonitoringApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SensorsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SensorsController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/Sensors
        [HttpPost]
        public async Task<ActionResult<Sensor>> CreateSensor(Sensor sensor)
        {
            _context.Sensors.Add(sensor);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSensor), new { id = sensor.Id }, sensor);
        }

        // GET: api/Sensors/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Sensor>> GetSensor(int id)
        {
            var sensor = await _context.Sensors.FindAsync(id);
            if (sensor == null) return NotFound();
            return sensor;
        }

        // POST: api/Sensors/link
        [HttpPost("link")]
        public async Task<IActionResult> LinkSensorToEquipment([FromBody] LinkSensorRequest request)
        {
            var sensor = await _context.Sensors.FirstOrDefaultAsync(s => s.Code == request.SensorCode);
            if (sensor == null) return NotFound("Sensor not found");

            var equipment = await _context.Equipments.FindAsync(request.EquipmentId);
            if (equipment == null) return NotFound("Equipment not found");

            sensor.Equipment = equipment;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Sensor linked to equipment successfully", sensor });
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class EquipmentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EquipmentsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<Equipment>> CreateEquipment(Equipment equipment)
        {
            _context.Equipments.Add(equipment);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetEquipment), new { id = equipment.Id }, equipment);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Equipment>> GetEquipment(int id)
        {
            var equipment = await _context.Equipments.FindAsync(id);
            if (equipment == null) return NotFound();
            return equipment;
        }

        [HttpGet("{id}/measurements")]
        public async Task<ActionResult<EquipmentMeasurementsDto>> GetEquipmentMeasurements(int id)
        {
            var equipment = await _context.Equipments.FindAsync(id);
            if (equipment == null) return NotFound();

            var sensors = await _context.Sensors
                .Where(s => s.EquipmentId == id)
                .ToListAsync();

            var result = new EquipmentMeasurementsDto
            {
                EquipmentId = equipment.Id,
                EquipmentName = equipment.Name,
                Sensors = new List<SensorMeasurementsDto>()
            };

            foreach (var sensor in sensors)
            {
                var measurements = await _context.Measurements
                    .Where(m => m.Codigo == sensor.Code)
                    .OrderByDescending(m => m.DataHoraMedicao)
                    .Take(10)
                    .ToListAsync();

                result.Sensors.Add(new SensorMeasurementsDto
                {
                    SensorId = sensor.Id,
                    SensorCode = sensor.Code,
                    Measurements = measurements
                });
            }

            return result;
        }
    }

    public class LinkSensorRequest
    {
        public string SensorCode { get; set; } = string.Empty;
        public int EquipmentId { get; set; }
    }

    public class EquipmentMeasurementsDto
    {
        public int EquipmentId { get; set; }
        public string EquipmentName { get; set; } = string.Empty;
        public List<SensorMeasurementsDto> Sensors { get; set; } = new();
    }

    public class SensorMeasurementsDto
    {
        public int SensorId { get; set; }
        public string SensorCode { get; set; } = string.Empty;
        public List<Measurement> Measurements { get; set; } = new();
    }
}
