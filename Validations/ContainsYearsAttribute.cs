using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;

namespace VehicleQuotes.Validation
{
    // In the .NET Framework, attribute classes need to have their name suffixed with the word "Attribute".
    // Validation attributes need to inherit from `System.ComponentModel.DataAnnotations`'s `ValidationAttribute` class
    // and override the `IsValid` method.
    public class ContainsYearsAttribute : ValidationAttribute
    {
        private string propertyName;

        // This constructor is called by the framework when the attribute is applied to some member. In this specific
        // case, we define a `propertyName` parameter annotated with a `CallerMemberName` attribute. This makes it so
        // the framework sends in the name of the member to which our `ContainsYears` attribute is applied to.
        // We store the value to use it later when constructing our validation error message.
        // Check https://docs.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.callermembernameattribute?view=net-5.0
        // for more info on `CallerMemberName`.
        public ContainsYearsAttribute([CallerMemberName] string propertyName = null)
        {
            this.propertyName = propertyName;
        }

        // This method is called by the framework during validation. `value` is the actual value of the field that this
        // attribute will validate.
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            // By only applying the validation checks when the value is not null, we make it possible for this
            // attribute to work on optional fields. In other words, this attribute will skip validation if there is no
            // value to validate.
            if (value != null)
            {
                // Check if all the elements of the string array are valid years. Check the `IsValidYear` method below
                // to see what checks are applied for each of the array elements.
                var isValid = (value as string[]).All(IsValidYear);

                if (!isValid)
                {
                    // If not, return an error.
                    return new ValidationResult(GetErrorMessage());
                }
            }

            // Return a successful validation result if no errors were detected.
            return ValidationResult.Success;
        }

        // Determines if a given value is valid by making sure it's not null, nor empty, that its length is 4 and that
        // all its characters are digits.
        private bool IsValidYear(string value) =>
            !String.IsNullOrEmpty(value) && value.Length == 4 && value.All(Char.IsDigit);

        // Builds a user friendly error message which includes the name of the field that this validation attribute has
        // been applied to.
        private string GetErrorMessage() =>
            $"The {propertyName} field must be an array of strings containing four digits.";
    }
}
