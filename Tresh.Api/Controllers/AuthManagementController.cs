using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
using Tresh.Dal;
using Tresh.Domain.Tokens;

namespace Tresh.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthManagementController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtConfig _jwtConfig;
        private readonly TokenValidationParameters _tokenValidationParams;
        private readonly DataContext _context;

        public AuthManagementController(UserManager<IdentityUser> userManager,
            IOptionsMonitor<JwtConfig> optionsMonitor,
            TokenValidationParameters tokenValidationParams,
            DataContext context)
        {
            _userManager = userManager;
            _jwtConfig = optionsMonitor.CurrentValue;
            _tokenValidationParams = tokenValidationParams;
            _context = context;
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
                var newUser = new IdentityUser { Email = user.Email, UserName = user.UserName, PasswordHash = user.Password.GetHashCode().ToString() };

                var isCreated = await _userManager.CreateAsync(newUser);

                if (isCreated.Succeeded)
                {
                    var jwtToken = await CreateJwtToken(newUser);

                    return Ok(jwtToken);
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

                //var isCorrect = await _userManager.CheckPasswordAsync(existingUser, user.Password.GetHashCode().ToString());

                var isCorrect = existingUser.PasswordHash == user.Password.GetHashCode().ToString();

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

                var jwtToken = await CreateJwtToken(existingUser);

                return Ok(jwtToken);
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
        [Route("RefreshToken")]
        public async Task<IActionResult> RefreshController([FromBody] TokenRequest token)
        {
            if (ModelState.IsValid)
            {
                var result = VerifyAndGenerateToken(token);

                if(result == null)
                {
                    return BadRequest(new AuthResult
                    {
                        IsSuccess = false,
                        Errors = new List<string>
                        {
                            "Invalid token"
                        }
                    });
                }

                return Ok(result);
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

        private async Task<AuthResult> CreateJwtToken(IdentityUser user)
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
                Expires = DateTime.UtcNow.AddMinutes(5),

                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
            };

            var token = jwtTokenHandler.CreateToken(tokenDescription);
            var jwtToken = jwtTokenHandler.WriteToken(token);

            var refreshToken = new RefreshToken
            {
                JwtId = token.Id,
                IsUsed = false,
                IsRevorked = false,
                UserId = user.Id,
                AddedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMonths(6),
                Token = RandomString(35) + Guid.NewGuid().ToString(),
            };

            _context.RefreshTokens.Add(refreshToken);


            await _context.SaveChangesAsync();

            return new AuthResult
            {
                Token = jwtToken,
                IsSuccess = true,
                RefreshToken = refreshToken.Token
            };
        }

        private async Task<AuthResult> VerifyAndGenerateToken(TokenRequest token)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            try
            {
                //Validation 1
                var tokenVerification = jwtTokenHandler.ValidateToken(token.Token, 
                    _tokenValidationParams, out var validatedToken);
                //Validation 2
                if(validatedToken is JwtSecurityToken jwtSecurityToken)
                {
                    var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256Signature);

                    if (result == false)
                        return null;
                }
                //Validation 3
                var utcExpiryDate = long.Parse(tokenVerification.Claims
                    .FirstOrDefault(t => t.Type == JwtRegisteredClaimNames.Exp).Value);

                var expiryDate = UnixTimeToDateTime(utcExpiryDate);

                if (expiryDate > DateTime.Now)
                    return new AuthResult
                    {
                        IsSuccess = false,
                        Errors = new List<string>
                        {
                            "Token has not yet expired"
                        }
                    };

                //validation 4
                var storedToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(x => x.Token == token.RefreshToken);

                if(storedToken == null)
                {
                    return new AuthResult
                    {
                        IsSuccess = false,
                        Errors = new List<string>
                        {
                            "Token does not exist"
                        }
                    };
                }

                //V 5
                if (storedToken.IsUsed)
                {
                    return new AuthResult
                    {
                        IsSuccess = false,
                        Errors = new List<string>
                        {
                            "Token has been used"
                        }
                    };
                }

                //V6
                if (storedToken.IsRevorked)
                {
                    return new AuthResult
                    {
                        IsSuccess = false,
                        Errors = new List<string>
                        {
                            "Token has been revoked"
                        }
                    };
                }


                //V 7
                var jti = tokenVerification.Claims
                    .FirstOrDefault(j => j.Type == JwtRegisteredClaimNames.Jti).Value;

                if(storedToken.JwtId != jti)
                {
                    return new AuthResult
                    {
                        IsSuccess = false,
                        Errors = new List<string>
                        {
                            "Token doesn't not match"
                        }
                    };
                }

                //update Token
                storedToken.IsUsed = true;
                _context.RefreshTokens.Update(storedToken);
                await _context.SaveChangesAsync();

                var dbUser = await _userManager.FindByIdAsync(storedToken.UserId);
                return await CreateJwtToken(dbUser);
            }

            catch (Exception e)
            {
                return null;
            }
        }

        private DateTime UnixTimeToDateTime(long utcExpiryDate)
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(utcExpiryDate).ToLocalTime();
            return dateTime;
        }

        private string RandomString(int length)
        {
            var random = new Random();
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(x => x[random.Next(x.Length)])
                .ToArray());            
        }
    }
}
