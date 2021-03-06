﻿using RdwTechdayRegistration.ValidationHelpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace RdwTechdayRegistration.Models.AccountViewModels
{
    public class RegisterViewModel : IValidatableObject
    {
        [Required]
        [EmailAddress]
        [Display(Name = "RDW e-mail")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Voor- en Achternaam")]
        public string Name { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Het {0} moet minimaal {2} en maximaal {1} karakters lang zijn.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password, gebruik hiervoor niet uw RDW wachtwoord")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Herhaal password")]
        [Compare("Password", ErrorMessage = "De wachtwoorden komen niet overeen.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "Divisie")]
        public string Department { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!EmailValidator.IsValid(Email))
            {
                yield return new ValidationResult(
                    $"Vul een e-mail adres in", new[] { "Email" });
            }
        }

    }
}
