﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RdwTechdayRegistration.Data;
using RdwTechdayRegistration.Models;
using RdwTechdayRegistration.Models.AccountViewModels;
using RdwTechdayRegistration.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RdwTechdayRegistration.Controllers
{
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            ILogger<AccountController> logger, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
            _context = context;
        }

        [TempData]
        public string ErrorMessage { get; set; }

        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User)) {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
            else
            {
                // Clear the existing external cookie to ensure a clean login process
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                // Require the user to have a confirmed email before they can log on.
                var user = await _userManager.FindByNameAsync(model.Email);
                if (user != null)
                {
                    if (!await _userManager.IsEmailConfirmedAsync(user))
                    {
                        ModelState.AddModelError(string.Empty, "U dient eerst uw e-mail adres te bevestigen voordat u in kunt loggen. Zie de mail die u ontvangen heeft na de inschrijving");
                        return View(model);
                    }
                }
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return RedirectToLocal(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToAction(nameof(LoginWith2fa), new { returnUrl, model.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToAction(nameof(Lockout));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> LoginWith2fa(bool rememberMe, string returnUrl = null)
        {
            // Ensure the user has gone through the username & password screen first
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            var model = new LoginWith2faViewModel { RememberMe = rememberMe };
            ViewData["ReturnUrl"] = returnUrl;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model, bool rememberMe, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, model.RememberMachine);

            if (result.Succeeded)
            {
                _logger.LogInformation("User with ID {UserId} logged in with 2fa.", user.Id);
                return RedirectToLocal(returnUrl);
            }
            else if (result.IsLockedOut)
            {
                _logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                _logger.LogWarning("Invalid authenticator code entered for user with ID {UserId}.", user.Id);
                ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> LoginWithRecoveryCode(string returnUrl = null)
        {
            // Ensure the user has gone through the username & password screen first
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            ViewData["ReturnUrl"] = returnUrl;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWithRecoveryCode(LoginWithRecoveryCodeViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            var recoveryCode = model.RecoveryCode.Replace(" ", string.Empty);

            var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

            if (result.Succeeded)
            {
                _logger.LogInformation("User with ID {UserId} logged in with a recovery code.", user.Id);
                return RedirectToLocal(returnUrl);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                _logger.LogWarning("Invalid recovery code entered for user with ID {UserId}", user.Id);
                ModelState.AddModelError(string.Empty, "Invalid recovery code entered.");
                return View();
            }
        }

        [HttpGet]
        public IActionResult Lockout()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult RegisterNonRdw(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterNonRdw(RegisterNonRdwViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email, Name = model.Name, Organisation = model.Organisation };
                user.isRDW = false;

                var password = "RTD-1" + Guid.NewGuid().ToString("d").Substring(1, 15);

                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "User");
                    await user.AddTijdvakkenAsync(_context);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("User created a new account with password.");

                    var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var callbackUrl = Url.RegisterNonRDWCallbackLink(user.Id, code, Request.Scheme);
                    var loginurl = Url.LoginLink(Request.Scheme);
                    var privacyurl = Url.PrivacyLink(Request.Scheme);
                    string htmlMessage = $"Beste {user.Name},<br/><br/>Wij hebben een account voor u aangemaakt waarmee u zich kunt registreren voor sessies op de RDW Techday. Om toegang te krijgen, dient u een nieuw wachtwoord in te stellen via de volgende <a href='{callbackUrl}'>link</a>." +
                       $"<br/><br/>LET OP: Bovenstaande link werkt eenmalig. Indien u deze mail al heeft bevestigd, dan kan kan u <a href='{loginurl}'>hier inloggen</a>." +
                       $"<br/><br/><br/>Met vriendelijke groet,<br/><br/>RDW Techday" +
                       $"<br/><br/><br/>Zie ook onze <a href='{privacyurl}'>privacyverklaring</a>";

                    string plainMessage = $"Beste {user.Name}" + Environment.NewLine + Environment.NewLine +
                       $"Wij hebben een account voor u aangemaakt waarmee u zich kunt registreren voor sessies op de RDW Techday. Om toegang te krijgen, dient u een nieuw wachtwoord in te stellen via de volgende link: {callbackUrl}" + Environment.NewLine + Environment.NewLine +
                       $"LET OP: Bovenstaande link werkt eenmalig. Indien u deze mail al heeft bevestigd, dan kan kan u hier inloggen: {loginurl}" + Environment.NewLine + Environment.NewLine + Environment.NewLine +
                       $"Met vriendelijke groet," + Environment.NewLine + Environment.NewLine +
                       $"RDW Techday" + Environment.NewLine + Environment.NewLine +
                       $"Zie ook onze privacyverklaring: {privacyurl}";

                    await _emailSender.SendEmailAsync(model.Email, "Welkom bij de RDW Techday", plainMessage, htmlMessage);
                    return Redirect(nameof(RegisterNonRdwConfirmation));
                }
                AddErrors(result);
            }
            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        public IActionResult RegisterNonRdwConfirmation()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> RegisterNonRdwCallback(string code = null)
        {
            if (code == null)
            {
                return View("AccessDenied");
            }
            if (User.Identity.IsAuthenticated)
            {
                // strange case found by a tester where a user was logged in and used a reset link
                // this will bork the reset system
                // log out and starting this method afresh will ensure a correct processing of the request
                await _signInManager.SignOutAsync();
                return RedirectToAction(nameof(RegisterNonRdwCallback), new { code = code });
            }
            var model = new ResetPasswordViewModel { Code = code };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterNonRdwCallback(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                // sign to prevent strange effects
                var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
                if (!result.Succeeded)
                {
                    AddErrors(result);
                    return View(model);
                }
                // reset lockout too
                await _userManager.SetLockoutEnabledAsync(user, false);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                await _userManager.ConfirmEmailAsync(user, code);
                var signingresult = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, lockoutOnFailure: true);
                return Redirect(Url.Content("~/"));
            }
            return View(model);
        }

        private void PopulateDivisiesDropDownList(object selectedDivisie = null)
        {

            List<string> divisies = new List<string> { "D&S", "ICT", "R&I", "T&B", "VRT" };
            ViewBag.Divisies = new SelectList(divisies, selectedDivisie);
        }

        [HttpGet]
        [Authorize(Policy = "SiteNotLocked")]
        public async Task<IActionResult> Register(string returnUrl = null)
        {
            bool isFull = await ApplicationUser.HasReachedMaxRdwOrSiteLocked(_context);
            if (!isFull)
            {
                ViewData["ReturnUrl"] = returnUrl;
                PopulateDivisiesDropDownList();
                return View();
            } else
            {
                TempData["StatusMessage"] = "Fout: Het maximum aantal inschrijvingen is bereikt of er kunnen geen inschrijvingen meer bij";
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        [HttpPost]
        [Authorize(Policy = "SiteNotLocked")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (model.Email.Length < 7 || model.Email.ToUpper().Substring(model.Email.Length - 7) != "@RDW.NL")
            {
                ModelState.AddModelError("Email", "Vul een RDW e-mail adres in. Bent u geen RDW-er, maar is uw organisatie wel lid van Samenwerking Noord?. Schrijf u dan in via samenwerkingnoord.nl");
            }
            if (ModelState.IsValid)
            {
                bool isFull = await ApplicationUser.HasReachedMaxRdwOrSiteLocked(_context);
                if (!isFull)
                {
                    var user = new ApplicationUser { UserName = model.Email, Email = model.Email, Name = model.Name, Organisation = "RDW", Department = model.Department };
                    user.isRDW = true;
                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "User");
                        await user.AddTijdvakkenAsync(_context);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("User created a new account with password.");

                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);
                        await _emailSender.SendEmailConfirmationAsync(user.Name, model.Email, callbackUrl);

                        _logger.LogInformation("User created a new account with password.");
                        return RedirectToAction(nameof(RegisterConfirmation));
                    }
                    AddErrors(result);
                }
                else
                {
                    ModelState.AddModelError("", "Het maximum aantal inschrijvingen is bereikt of inschrijven niet meer mogelijk");
                }
            }
            PopulateDivisiesDropDownList(model.Department);
            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        public IActionResult RegisterConfirmation()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User signed out.");
            return RedirectToAction("SignedOut");
        }

        [HttpGet]
        public IActionResult SignedOut()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToAction(nameof(Login));
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in with {Name} provider.", info.LoginProvider);
                return RedirectToLocal(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                // If the user does not have an account, then ask the user to create an account.
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["LoginProvider"] = info.LoginProvider;
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                return View("ExternalLogin", new ExternalLoginViewModel { Email = email });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    throw new ApplicationException("Error loading external login information during confirmation.");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(nameof(ExternalLogin), model);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            bool isFull = await ApplicationUser.HasReachedMaxRdwOrSiteLocked(_context);
            if (isFull)
            {
                TempData["StatusMessage"] = "Fout: Het maximum aantal inschrijvingen is bereikt of inschrijven is niet meer mogelijk";
            }
            if (userId == null || code == null || isFull )
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{userId}'.");
            }
            var result = await _userManager.ConfirmEmailAsync(user, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.ResetPasswordCallbackLink(user.Id, code, Request.Scheme);
                string plainMessage = $"Please reset your password by clicking here: {callbackUrl}";
                string htmlMessage = $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>";
                await _emailSender.SendEmailAsync(model.Email, "Reset Password", plainMessage, htmlMessage);
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string code = null)
        {
            if (code == null)
            {
                return View("AccessDenied");
            }
            if (User.Identity.IsAuthenticated)
            {
                // strange case found by a tester where a user was logged in and used a reset link
                // this will bork the reset system
                // log out and starting this method afresh will ensure a correct processing of the request
                await _signInManager.SignOutAsync();
                return RedirectToAction(nameof(ResetPassword), new { code = code });
            }
            var model = new ResetPasswordViewModel { Code = code };
            //return RedirectToAction("Action", new { id = 99 });
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                // sign to prevent strange effects
                var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
                if (result.Succeeded)
                {
                    // reset lockout
                    await _userManager.SetLockoutEnabledAsync(user, false);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    await _userManager.ConfirmEmailAsync(user, code);
                    var signingresult = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, lockoutOnFailure: true);
                    return RedirectToAction(nameof(ResetPasswordConfirmation));
                }
                AddErrors(result);
            }
            ModelState.AddModelError("Email", "Er zit een fout in de door u verstrekte gegevens. Is het e-mail adres dezelfde als waar u de u de link op ontvangen hebt?");
            return View();
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        #endregion Helpers
    }
}