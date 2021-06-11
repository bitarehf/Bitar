using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using Bitar.Models;
using Bitar.Models.Dilisense;
using Bitar.Models.JaModels;
using Bitar.Models.Settings;
using Bitar.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NBitcoin;

namespace Bitar.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly ILogger<AccountController> _logger;
        private readonly JwtSettings _options;
        private readonly JaSettings _jaOptions;
        private readonly DilisenseSettings _dilisenseOptions;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly BitcoinService _bitcoin;
        private readonly IConfiguration _configuration;

        public AccountController(
            ILogger<AccountController> logger,
            IOptions<JwtSettings> options,
            IOptions<JaSettings> jaOptions,
            IOptions<DilisenseSettings> dilisenseOptions,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            BitcoinService bitcoin,
            IConfiguration configuration
        )
        {
            _logger = logger;
            _options = options.Value;
            _jaOptions = jaOptions.Value;
            _dilisenseOptions = dilisenseOptions.Value;
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _bitcoin = bitcoin;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<ActionResult> Login(LoginDTO login)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var user = await _userManager.FindByIdAsync(login.User);
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(login.User);
                if (user == null)
                {
                    return Unauthorized();
                }
            }

            // Ensure account data has been created incase they
            // failed to be created when the account was registered.
            //await CreateAccountData(user.Id);

            // Check the password but don't "sign in" (which would set a cookie).
            var result = await _signInManager.CheckPasswordSignInAsync(user, login.Password, false);
            if (result.Succeeded)
            {
                var principal = await _signInManager.CreateUserPrincipalAsync(user);
                var token = new JwtSecurityToken(
                    issuer: _options.JwtIssuer,
                    audience: _options.JwtIssuer,
                    claims: principal.Claims,
                    expires: DateTime.UtcNow.AddDays(_options.JwtExpireDays),
                    signingCredentials: new SigningCredentials(new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(_options.JwtKey)),
                        SecurityAlgorithms.HmacSha256
                    ));

                _logger.LogCritical(login.User + " logged in");

                return Ok(new JwtSecurityTokenHandler().WriteToken(token));
            }

            return Unauthorized();
        }

        [HttpPost]
        public async Task<ActionResult> Island([FromForm] TokenDTO tokenDTO)
        {
            _logger.LogInformation("yeet token yeet");
            IslandLogin.IslandLogin token = new IslandLogin.IslandLogin(tokenDTO.Token);
            _logger.LogInformation(token.Token);
            _logger.LogInformation(token.Island.UserId);
            _logger.LogInformation(token.Island.Name);
            _logger.LogInformation(token.Island.Authentication);

            bool verified = token.Verify();

            if (verified)
            {
                var user = await _userManager.FindByIdAsync(token.Island.UserId);

                if (user == null)
                {
                    return Unauthorized();
                }

                user.IdConfirmed = true;
                user.UserName = token.Island.Name;
                user.PhoneNumberConfirmed = true;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return Redirect("https://bitar.is/dashboard");
                }
                else
                {
                    return BadRequest();
                }
            }
            else
            {
                return Unauthorized();
            }
        }

        // This needs cleaning, I was in a rush.
        [HttpPost]
        public async Task<ActionResult> Register(RegisterDTO register)
        {
            // Don't try to create a user that already exists.
            if (await _context.Users.FindAsync(register.Id) != null)
            {
                _logger.LogWarning($"Account already exists. Account id: {register.Id}");
                return Conflict("Account already exists");
            }

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", _jaOptions.ApiKey);
            var kennitalaRequest = await client.GetAsync($"https://api.ja.is/skra/v1/kennitolur/{register.Id}");
            if (!kennitalaRequest.IsSuccessStatusCode)
            {
                _logger.LogCritical("Ja API is not working!");
            }

            KennitalaOverview kennitala = JsonSerializer.Deserialize<KennitalaOverview>(
                await kennitalaRequest.Content.ReadAsStringAsync());

            bool institution = false;
            if (kennitala.KennitalaType == "business")
            {
                institution = true;
            }

            var personRequest = await client.GetAsync($"https://api.ja.is/skra/v1/people/{register.Id}");
            if (!personRequest.IsSuccessStatusCode)
            {
                _logger.LogCritical("Ja API is not working!");
            }

            Person p = JsonSerializer.Deserialize<Person>(
                await personRequest.Content.ReadAsStringAsync());

            HttpClient c = new HttpClient();
            client.DefaultRequestHeaders.Add("x-api-key", _dilisenseOptions.ApiKey);
            string url = Uri.EscapeUriString($"https://api.dilisense.com/v1/checkIndividual?names={p.Name}");
            _logger.LogInformation(url);
            var dilisense = await c.GetAsync(url);
            if (!dilisense.IsSuccessStatusCode)
            {
                _logger.LogCritical("Dilisense API is not working!");
            }

            Individual individual = JsonSerializer.Deserialize<Individual>(
                await dilisense.Content.ReadAsStringAsync());

            // Temp for testing.
            bool politicallyExposed = false;
            if (individual.TotalHits >= 1)
            {
                politicallyExposed = true;
            }

            var user = new ApplicationUser
            {
                Id = register.Id,
                UserName = p.Name,
                Email = register.Email,
                RegistrationDate = DateTime.Now,
                Institution = institution,
                PostalCode = p.PermanentAddress.PostalCode.ToString(),
                Address = p.PermanentAddress.Street.Dative,
                PoliticallyExposed = politicallyExposed, // Temp for testing.
                SanctionList = false, // Temp for testing.
                CriminalWatchlist = false // Temp for testing.
            };

            _logger.LogDebug(
                $"Id: {register.Id}\n" +
                $"Email: {register.Email}\n" +
                $"Password: {register.Password}");

            var result = await _userManager.CreateAsync(user, register.Password);

            if (result.Succeeded)
            {
                await CreateAccountData(register.Id);

                var login = new LoginDTO()
                {
                    User = register.Id,
                    Password = register.Password
                };

                return await Login(login);
            }

            return NotFound();
        }

        private async Task CreateAccountData(string id)
        {
            try
            {
                // Don't try to create account data if it already exists.
                if (await _userManager.FindByIdAsync(id) == null) return;

                var accountData = new AccountData
                {
                    Id = id,
                    Fee = 0.5m
                    // Derivation is automatically assigned an id.
                };

                await _context.AccountData.AddAsync(accountData);
                await _context.SaveChangesAsync();

                var address = await _bitcoin.GetDepositAddress(id);
                // Import Address to bitcoin node to keep track of it.
                //await _bitcoin.ImportAddress(address, id);

            }
            catch (WebException)
            {
                _logger.LogCritical("Failed to import address to bitcoin node. Is the bitcoin node down?");
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
        }

        [Authorize]
        [HttpGet]
        public ActionResult<string> Protected()
        {
            _logger.LogCritical($"{User.FindFirstValue(ClaimTypes.NameIdentifier)} accessed the protected area");
            return "Protected area";
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<string>> GetUserEmail()
        {
            string id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("User not found");
            }

            return Ok(user.Email);
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<string>> GetUserName()
        {
            string id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("User not found");
            }

            return Ok(user.UserName);
        }

        // private async Task<object> GenerateJwtToken(string email, IdentityUser user)
        // {
        //     var claims = new List<Claim>
        //     {
        //         new Claim(JwtRegisteredClaimNames.Sub, email),
        //         new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        //         new Claim(ClaimTypes.NameIdentifier, user.Id)
        //     };

        //     var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtKey"]));
        //     var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        //     var expires = DateTime.Now.AddDays(Convert.ToDouble(_configuration["JwtExpireDays"]));

        //     var token = new JwtSecurityToken(
        //         _configuration["JwtIssuer"],
        //         _configuration["JwtIssuer"],
        //         claims,
        //         expires: expires,
        //         signingCredentials: creds
        //     );

        //     return new JwtSecurityTokenHandler().WriteToken(token);
        // }

        public class TokenDTO
        {
            [Required]
            public string Token { get; set; }
        }

        public class LoginDTO
        {
            [Required]
            public string User { get; set; }

            [Required]
            public string Password { get; set; }

        }

        public class RegisterDTO
        {
            [Required]
            [StringLength(10, ErrorMessage = "ID_LENGTH", MinimumLength = 10)]
            public string Id { get; set; }

            [Required]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "PASSWORD_LENGTH", MinimumLength = 6)]
            public string Password { get; set; }
        }
    }
}