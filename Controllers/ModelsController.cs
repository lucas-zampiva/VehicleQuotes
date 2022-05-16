using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleQuotes.Data;
using VehicleQuotes.Models;
using VehicleQuotes.ResourceModels;

namespace VehicleQuotes.Controllers
{
    [Route("api/Makes/{makeId}/[controller]/")]
    [ApiController]
    public class ModelsController : ControllerBase
    {
        private readonly VehicleQuotesContext _context;

        public ModelsController(VehicleQuotesContext context)
        {
            _context = context;
        }

        // GET: api/Models
        [HttpGet]
        // Return a collection of `ModelSpecification`s and expect a `makeId` from the URL.
        public async Task<ActionResult<IEnumerable<ModelSpecification>>> GetModels([FromRoute] int makeId)
        {
            // Look for the make identified by `makeId`.
            var make = await _context.Makes.FindAsync(makeId);

            // If we can't find the make, then we return a 404.
            if (make == null)
            {
                return NotFound();
            }

            // Build a query to fetch the relevant records from the `models` table and
            // build `ModelSpecification` with the data.
            var modelsToReturn = _context.Models
                .Where(m => m.MakeID == makeId)
                .Select(m => new ModelSpecification
                {
                    ID = m.ID,
                    Name = m.Name,
                    Styles = m.ModelStyles.Select(ms => new ModelSpecificationStyle
                    {
                        BodyType = ms.BodyType.Name,
                        Size = ms.Size.Name,
                        Years = ms.ModelStyleYears.Select(msy => msy.Year).ToArray()
                    }).ToArray()
                });

            // Execute the query and respond with the results.
            return await modelsToReturn.ToListAsync();
        }

        // GET: api/Models/5
        [HttpGet("{id}")]
        // Return a `ModelSpecification`s and expect `makeId` and `id` from the URL.
        public async Task<ActionResult<ModelSpecification>> GetModel([FromRoute] int makeId, [FromRoute] int id)
        {
            // Look for the model specified by the given identifiers and also load
            // all related data that we care about for this method.
            var model = await _context.Models
                .Include(m => m.ModelStyles).ThenInclude(ms => ms.BodyType)
                .Include(m => m.ModelStyles).ThenInclude(ms => ms.Size)
                .Include(m => m.ModelStyles).ThenInclude(ms => ms.ModelStyleYears)
                .FirstOrDefaultAsync(m => m.MakeID == makeId && m.ID == id);

            // If we couldn't find it, respond with a 404.
            if (model == null)
            {
                return NotFound();
            }

            // Use the fetched data to construct a `ModelSpecification` to use in the response.
            return new ModelSpecification
            {
                ID = model.ID,
                Name = model.Name,
                Styles = model.ModelStyles.Select(ms => new ModelSpecificationStyle
                {
                    BodyType = ms.BodyType.Name,
                    Size = ms.Size.Name,
                    Years = ms.ModelStyleYears.Select(msy => msy.Year).ToArray()
                }).ToArray()
            };
        }

        // PUT: api/Models/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        // Expect `makeId` and `id` from the URL and a `ModelSpecification` from the request payload.
        public async Task<IActionResult> PutModel([FromRoute] int makeId, int id, ModelSpecification model)
        {
            // If the id in the URL and the request payload are different, return a 400.
            if (id != model.ID)
            {
                return BadRequest();
            }

            // Obtain the `models` record that we want to update. Include any related
            // data that we want to update as well.
            var modelToUpdate = await _context.Models
                .Include(m => m.ModelStyles)
                .FirstOrDefaultAsync(m => m.MakeID == makeId && m.ID == id);

            // If we can't find the record, then return a 404.
            if (modelToUpdate == null)
            {
                return NotFound();
            }

            // Update the record with what came in the request payload.
            modelToUpdate.Name = model.Name;

            // Build EF Core entities based on the incoming Resource Model object.
            modelToUpdate.ModelStyles = model.Styles.Select(style => new ModelStyle
            {
                BodyType = _context.BodyTypes.Single(bodyType => bodyType.Name == style.BodyType),
                Size = _context.Sizes.Single(size => size.Name == style.Size),

                ModelStyleYears = style.Years.Select(year => new ModelStyleYear
                {
                    Year = year
                }).ToList()
            }).ToList();

            try
            {
                // Try saving the changes. This will run the UPDATE statement in the database.
                await _context.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                // If there's an error updating, respond accordingly.
                return Conflict();
            }

            // Finally return a 204 if everything went well.
            return NoContent();
        }

        /// <summary>
        /// Creates a new vehicle model for the given make.
        /// </summary>
        /// <param name="makeId">The ID of the vehicle make to add the model to.</param>
        /// <param name="model">The data to create the new model with.</param>
        /// <response code="201">When the request is valid.</response>
        /// <response code="404">When the specified vehicle make does not exist.</response>
        /// <response code="409">When there's already another model in the same make with the same name.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ModelSpecification>> PostModel([FromRoute] int makeId, ModelSpecification model)
        {
            // First, try to find the make specified by the incoming `makeId`.
            var make = await _context.Makes.FindAsync(makeId);

            // Respond with 404 if not found.
            if (make == null)
            {
                return NotFound();
            }

            // Build out a new `Model` entity, complete with all related data, based on
            // the `ModelSpecification` parameter.
            var modelToCreate = new Model
            {
                Make = make,
                Name = model.Name,

                ModelStyles = model.Styles.Select(style => new ModelStyle
                {
                    // Notice how we search both body type and size by their name field.
                    // We can do that because their names are unique.
                    BodyType = _context.BodyTypes.Single(bodyType => bodyType.Name == style.BodyType),
                    Size = _context.Sizes.Single(size => size.Name == style.Size),

                    ModelStyleYears = style.Years.Select(year => new ModelStyleYear
                    {
                        Year = year
                    }).ToArray()
                }).ToArray()
            };

            // Add it to the DbContext.
            _context.Add(modelToCreate);

            try
            {
                // Try running the INSERTs.
                await _context.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                // Return accordingly if an error happens.
                return Conflict();
            }

            // Get back the autogenerated ID of the record we just INSERTed.
            model.ID = modelToCreate.ID;

            // Finally, return a 201 including a location header containing the newly
            // created resource's URL and the resource itself in the response payload.
            return CreatedAtAction(
                nameof(GetModel),
                new { makeId = makeId, id = model.ID },
                model
            );
        }

        // DELETE: api/Models/5
        [HttpDelete("{id}")]
        // Expect `makeId` and `id` from the URL.
        public async Task<IActionResult> DeleteModel([FromRoute] int makeId, int id)
        {
            // Try to find the record identified by the ids from the URL.
            var model = await _context.Models.FirstOrDefaultAsync(m => m.MakeID == makeId && m.ID == id);

            // Respond with a 404 if we can't find it.
            if (model == null)
            {
                return NotFound();
            }

            // Mark the entity for removal and run the DELETE.
            _context.Models.Remove(model);
            await _context.SaveChangesAsync();

            // Respond with a 204.
            return NoContent();
        }

        private bool ModelExists(int id)
        {
            return _context.Models.Any(e => e.ID == id);
        }
    }
}
