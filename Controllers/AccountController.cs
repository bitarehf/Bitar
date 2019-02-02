using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Bitar.Models;
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
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly BitcoinService _bitcoinService;
        private readonly IConfiguration _configuration;

        public AccountController(
            ILogger<AccountController> logger,
            IOptions<JwtSettings> options,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            BitcoinService bitcoinService,
            IConfiguration configuration
            )
        {
            _logger = logger;
            _options = options.Value;
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _bitcoinService = bitcoinService;
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

            // Ensure account details have been created incase they
            // failed to be created when the account was registered.
            await CreateAccountData(user.Id);

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
        public async Task<ActionResult> Register(RegisterDTO register)
        {
            // Don't try to create a user that already exists.
            if (await _context.Users.FindAsync(register.SSN) != null)
            {
                _logger.LogWarning($"Account already exists. Account id: {register.SSN}");
                return Conflict("Account already exists");
            }

            var user = new ApplicationUser
            {
                Id = register.SSN,
                UserName = register.SSN,
                Email = register.Email
            };

            _logger.LogInformation("Email:" + register.Email);
            _logger.LogInformation("Password:" + register.Password);
            _logger.LogInformation("SSN:" + register.SSN);

            var result = await _userManager.CreateAsync(user, register.Password);

            if (result.Succeeded)
            {
                await CreateAccountData(register.SSN);

                return Ok("Account created");
            }

            return NotFound();
        }

        private async Task CreateAccountData(string id)
        {
            try
            {
                // Don't try to create account data if it already exists.
                if (await _userManager.FindByIdAsync(id) == null) return;

                // Generate a random private key
                var privateKey = new Key();
                // Convert the private key to WIF.
                var bitcoinSecret = privateKey.GetWif(Network.Main);
                // This is the deposit address.
                var bech32 = bitcoinSecret.GetSegwitAddress();

                var accountData = new AccountData
                {
                    Id = id,
                    DepositAddress = bech32.ToString(),
                    BitcoinSecret = bitcoinSecret.ToWif()
                };

                // Import Address to bitcoin node in order to track transactions.
                await _bitcoinService.ImportAddress(bech32);

                // Add the keys we just created to account data.
                await _context.AccountData.AddAsync(accountData);
                await _context.SaveChangesAsync();

            }
            catch (WebException)
            {
                _logger.LogCritical("Failed to import address to bitcoin node.");
                _logger.LogCritical("AccountDetails not created for account.");
                _logger.LogCritical("Is the bitcoin node down?");
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
            return "Protected area";
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
            public string SSN { get; set; }

            [Required]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "PASSWORD_MIN_LENGTH", MinimumLength = 6)]
            public string Password { get; set; }
        }
    }
}