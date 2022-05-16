using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VehicleQuotes.Data;
using VehicleQuotes.Models;
using VehicleQuotes.ResourceModels;
using Microsoft.Extensions.Configuration;

namespace VehicleQuotes.Services
{
    public class QuoteService
    {
        private readonly VehicleQuotesContext _context;
        private readonly IConfiguration _configuration;

        // This constructor defines a dependency on VehicleQuotesContext, similar to most of our controllers.
        // Via the built in dependency injection features, the framework makes sure to provide this parameter when
        // creating new instances of this class.
        public QuoteService(VehicleQuotesContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // This method takes all the records from the `quotes` table and constructs `SubmittedQuoteRequest`s with them.
        // Then returns that as a list.
        public async Task<List<SubmittedQuoteRequest>> GetAllQuotes()
        {
            var quotesToReturn = _context.Quotes.Select(q => new SubmittedQuoteRequest
            {
                ID = q.ID,
                CreatedAt = q.CreatedAt,
                OfferedQuote = q.OfferedQuote,
                Message = q.Message,

                Year = q.Year,
                Make = q.Make,
                Model = q.Model,
                BodyType = q.BodyType.Name,
                Size = q.Size.Name,

                ItMoves = q.ItMoves,
                HasAllWheels = q.HasAllWheels,
                HasAlloyWheels = q.HasAlloyWheels,
                HasAllTires = q.HasAllTires,
                HasKey = q.HasKey,
                HasTitle = q.HasTitle,
                RequiresPickup = q.RequiresPickup,
                HasEngine = q.HasEngine,
                HasTransmission = q.HasTransmission,
                HasCompleteInterior = q.HasCompleteInterior,
            });

            return await quotesToReturn.ToListAsync();
        }

        // This method takes an incoming `QuoteRequest` and calculates a quote based on the vehicle described by it.
        // To calculate this quote, it looks for any overrides before trying to use the currently existing rules defined
        // in the `quote_rules` table. It also stores a record on the `quotes` table with all the incoming data and the
        // quote calculation result. It returns back the quote value as well as a message explaining the conditions of
        // the quote.
        public async Task<SubmittedQuoteRequest> CalculateQuote(QuoteRequest request)
        {
            var response = this.CreateResponse(request);
            var quoteToStore = await this.CreateQuote(request);
            var requestedModelStyleYear = await this.FindModelStyleYear(request);
            QuoteOverride quoteOverride = null;

            if (requestedModelStyleYear != null)
            {
                quoteToStore.ModelStyleYear = requestedModelStyleYear;

                quoteOverride = await this.FindQuoteOverride(requestedModelStyleYear);

                if (quoteOverride != null)
                {
                    response.OfferedQuote = quoteOverride.Price;
                }
            }

            if (quoteOverride == null)
            {
                response.OfferedQuote = await this.CalculateOfferedQuote(request);
            }

            if (requestedModelStyleYear == null)
            {
                response.Message = "Offer subject to change upon vehicle inspection.";
            }

            if (response.OfferedQuote <= 0)
            {
                response.OfferedQuote = _configuration.GetValue<int>("DefaultOffer", 0);
            }

            quoteToStore.OfferedQuote = response.OfferedQuote;
            quoteToStore.Message = response.Message;

            _context.Quotes.Add(quoteToStore);
            await _context.SaveChangesAsync();

            response.ID = quoteToStore.ID;
            response.CreatedAt = quoteToStore.CreatedAt;

            return response;
        }

        // Creates a `SubmittedQuoteRequest`, initialized with default values, using the data from the incoming
        // `QuoteRequest`. `SubmittedQuoteRequest` is what gets returned in the response payload of the quote endpoints.
        private SubmittedQuoteRequest CreateResponse(QuoteRequest request)
        {
            return new SubmittedQuoteRequest
            {
                OfferedQuote = 0,
                Message = "This is our final offer.",

                Year = request.Year,
                Make = request.Make,
                Model = request.Model,
                BodyType = request.BodyType,
                Size = request.Size,

                ItMoves = request.ItMoves,
                HasAllWheels = request.HasAllWheels,
                HasAlloyWheels = request.HasAlloyWheels,
                HasAllTires = request.HasAllTires,
                HasKey = request.HasKey,
                HasTitle = request.HasTitle,
                RequiresPickup = request.RequiresPickup,
                HasEngine = request.HasEngine,
                HasTransmission = request.HasTransmission,
                HasCompleteInterior = request.HasCompleteInterior,
            };
        }

        // Creates a `Quote` based on the data from the incoming `QuoteRequest`. This is the object that gets eventually
        // stored in the database.
        private async Task<Quote> CreateQuote(QuoteRequest request)
        {
            return new Quote
            {
                Year = request.Year,
                Make = request.Make,
                Model = request.Model,
                BodyTypeID = (await _context.BodyTypes.SingleAsync(bt => bt.Name == request.BodyType)).ID,
                SizeID = (await _context.Sizes.SingleAsync(s => s.Name == request.Size)).ID,

                ItMoves = request.ItMoves,
                HasAllWheels = request.HasAllWheels,
                HasAlloyWheels = request.HasAlloyWheels,
                HasAllTires = request.HasAllTires,
                HasKey = request.HasKey,
                HasTitle = request.HasTitle,
                RequiresPickup = request.RequiresPickup,
                HasEngine = request.HasEngine,
                HasTransmission = request.HasTransmission,
                HasCompleteInterior = request.HasCompleteInterior,

                CreatedAt = DateTime.Now
            };
        }

        // Tries to find a registered vehicle that matches the one for which the quote is currently being requested.
        private async Task<ModelStyleYear> FindModelStyleYear(QuoteRequest request)
        {
            return await _context.ModelStyleYears.FirstOrDefaultAsync(msy =>
                msy.Year == request.Year &&
                msy.ModelStyle.Model.Make.Name == request.Make &&
                msy.ModelStyle.Model.Name == request.Model &&
                msy.ModelStyle.BodyType.Name == request.BodyType &&
                msy.ModelStyle.Size.Name == request.Size
            );
        }

        // Tries to find an override for the vehicle for which the quote is currently being requested.
        private async Task<QuoteOverride> FindQuoteOverride(ModelStyleYear modelStyleYear)
        {
            return await _context.QuoteOverides
                .FirstOrDefaultAsync(qo => qo.ModelStyleYear == modelStyleYear);
        }

        // Uses the rules stored in the `quote_rules` table to calculate how much money to offer for the vehicle
        // described in the incoming `QuoteRequest`.
        private async Task<int> CalculateOfferedQuote(QuoteRequest request)
        {
            var rules = await _context.QuoteRules.ToListAsync();

            // Given a vehicle feature type, find a rule that applies to that feature type and has the value that
            // matches the condition of the incoming vehicle being quoted.
            Func<string, QuoteRule> theMatchingRule = featureType =>
                rules.FirstOrDefault(r =>
                    r.FeatureType == featureType &&
                    r.FeatureValue == request[featureType]
                );

            // For each vehicle feature that we care about, sum up the the monetary values of all the rules that match
            // the given vehicle condition.
            return QuoteRule.FeatureTypes.All
                .Select(theMatchingRule)
                .Where(r => r != null)
                .Sum(r => r.PriceModifier);
        }
    }
}
