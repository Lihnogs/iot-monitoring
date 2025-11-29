using Microsoft.AspNetCore.Mvc;
using IotMonitoringApi.Data;
using IotMonitoringApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IotMonitoringApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MeasurementsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MeasurementsController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/Measurements
        [HttpPost]
        public async Task<ActionResult<IEnumerable<Measurement>>> PostMeasurements(IEnumerable<Measurement> measurements)
        {
            if (measurements == null || !measurements.Any())
            {
                return BadRequest("No measurements provided.");
            }

            _context.Measurements.AddRange(measurements);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMeasurements), new { }, measurements);
        }

        // GET: api/Measurements
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Measurement>>> GetMeasurements()
        {
            return await _context.Measurements.ToListAsync();
        }
    }
}
