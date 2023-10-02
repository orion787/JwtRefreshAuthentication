using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Tresh.Api.Configuration;
using Tresh.Api.Dtos.Requests;
using Tresh.Api.Dtos.Responses;

namespace Tresh.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthManagementController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtConfig _jwtConfig;
        public AuthManagementController(UserManager<IdentityUser> userManager,
            IOptionsMonitor<JwtConfig> optionsMonitor)
        {
            _userManager = userManager;
            _jwtConfig = optionsMonitor.CurrentValue;
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto user)
        {
            if (ModelState.IsValid)
            {
                var existedUser = await _userManager.FindByEmailAsync(user.Email);

                if (existedUser != null)
                {
                    return BadRequest(new RegistrationResponse
                    {
                        Errors = new List<string>
                        {
                            "email already in use"
                        },
                        IsSuccess = false
                    });
                }
                var newUser = new IdentityUser { Email = user.Email, UserName = user.UserName };
                var isCreated = await _userManager.CreateAsync(newUser);

                if (isCreated.Succeeded)
                {
                    var jwtToken = CreateJwtToken(newUser);

                    return Ok(new RegistrationResponse
                    {
                        IsSuccess = true,
                        Token = jwtToken
                    });
                }
                else
                {
                    return BadRequest(new RegistrationResponse
                    {
                        Errors = isCreated.Errors.Select(x => x.Description).ToList(),
                        IsSuccess = false
                    });
                }

            }
            return BadRequest(new RegistrationResponse
            {
                Errors = new List<string>
                {
                    "Invalid payload"
                },
                IsSuccess = false
            });
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest user)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(user.Email);

                if (existingUser == null)
                {
                    return BadRequest(new RegistrationResponse
                    {
                        Errors = new List<string>
                        {
                            "Invalid login request"
                        },
                        IsSuccess = false
                    });
                }

                var isCorrect = await _userManager.CheckPasswordAsync(existingUser, user.Password);

                if (!isCorrect)
                {
                    return BadRequest(new RegistrationResponse
                    {
                        Errors = new List<string>
                        {
                            "Invalid login request"
                        },
                        IsSuccess = false
                    });
                }

                var jwtToken = CreateJwtToken(existingUser);

                return Ok(new RegistrationResponse
                {
                    IsSuccess = true,
                    Token = jwtToken
                });
            }

            return BadRequest(new RegistrationResponse
            {
                Errors = new List<string>
                {
                    "Invalid payload"
                },
                IsSuccess = false
            });
        }

        private string CreateJwtToken(IdentityUser user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);

            var tokenDescription = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("Id", user.Id),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(6),

                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
            };

            var token = jwtTokenHandler.CreateToken(tokenDescription);
            var jwtToken = jwtTokenHandler.WriteToken(token);

            return jwtToken;
        }
    }
}
