using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleQuotes.Data;
using VehicleQuotes.Models;

namespace VehicleQuotes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BodyTypesController : ControllerBase
    {
        private readonly VehicleQuotesContext _context;

        public BodyTypesController(VehicleQuotesContext context)
        {
            _context = context;
        }

        // GET: api/BodyTypes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BodyType>>> GetBodyTypes()
        {
            return await _context.BodyTypes.ToListAsync();
        }

        // GET: api/BodyTypes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BodyType>> GetBodyType(int id)
        {
            var bodyType = await _context.BodyTypes.FindAsync(id);

            if (bodyType == null)
            {
                return NotFound();
            }

            return bodyType;
        }

        // PUT: api/BodyTypes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBodyType(int id, BodyType bodyType)
        {
            if (id != bodyType.ID)
            {
                return BadRequest();
            }

            _context.Entry(bodyType).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BodyTypeExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/BodyTypes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<BodyType>> PostBodyType(BodyType bodyType)
        {
            _context.BodyTypes.Add(bodyType);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetBodyType", new { id = bodyType.ID }, bodyType);
        }

        // DELETE: api/BodyTypes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBodyType(int id)
        {
            var bodyType = await _context.BodyTypes.FindAsync(id);
            if (bodyType == null)
            {
                return NotFound();
            }

            _context.BodyTypes.Remove(bodyType);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool BodyTypeExists(int id)
        {
            return _context.BodyTypes.Any(e => e.ID == id);
        }
    }
}
