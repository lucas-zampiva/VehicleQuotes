using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VehicleQuotes.ResourceModels;
using VehicleQuotes.Services;

namespace VehicleQuotes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuotesController : ControllerBase
    {
        private readonly QuoteService _service;

        // When intiating the request processing logic, the framework recognizes
        // that this controller has a dependency on QuoteService and expects an
        // instance of it to be injected via the constructor. The framework then
        // does what it needs to do in order to provide that dependency.
        public QuotesController(QuoteService service)
        {
            _service = service;
        }

        // GET: api/Quotes
        [HttpGet]
        // This method returns a collection of a new resource model instead of just the `Quote` entity directly.
        public async Task<ActionResult<IEnumerable<SubmittedQuoteRequest>>> GetAll()
        {
            // Instead of directly implementing the logic in this method, we call on
            // the service class and let it take care of the rest.
            return await _service.GetAllQuotes();
        }

        // POST: api/Quotes
        [HttpPost]
        // This method receives as a paramater a `QuoteRequest` of just the `Quote` entity directly.
        // That way callers of this endpoint don't need to be exposed to the details of our data model implementation.
        public async Task<ActionResult<SubmittedQuoteRequest>> Post(QuoteRequest request)
        {
            // Instead of directly implementing the logic in this method, we call on
            // the service class and let it take care of the rest.
            return await _service.CalculateQuote(request);
        }
    }
}
