using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Bitar.Models;
using Bitar.Models.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Bitar.Controllers
{
    [Route("[action]")]
    public class AccountController : ControllerBase
    {
        private readonly ILogger<AccountController> _logger;
        private readonly JwtSettings _options;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public AccountController(
            ILogger<AccountController> logger,
            IOptions<JwtSettings> options,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration
            )
        {
            _logger = logger;
            _options = options.Value;
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<ActionResult> Login([FromBody] LoginDto login)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var user = await _userManager.FindByIdAsync(login.User);
            if (user == null)
            {
                user = await _userManager.FindByNameAsync(login.User);
                if (user == null)
                {
                    return Unauthorized();
                }
            }

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
        public async Task<ActionResult> Register([FromBody] RegisterDto register)
        {
            var user = new ApplicationUser
            {
                Id = register.SSN,
                Email = register.Email
            };
            var result = await _userManager.CreateAsync(user, register.Password);

            if (result.Succeeded)
            {
                return Ok("Account created");
            }

            return NotFound();
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

        public class LoginDto
        {
            [Required]
            public string User { get; set; }

            [Required]
            public string Password { get; set; }

        }

        public class RegisterDto
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