using AutoMapper;
using HotelListing.API.Data;
using HotelListing.API.Core.DTOs.Users;
using HotelListing.API.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HotelListing.API.Core.Repository
{
    public class AuthManager : IAuthManager
    {
        private readonly IMapper _mapper;
        private readonly UserManager<ApiUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthManager> _logger;
        private ApiUser _user;

        private const string _loginProvider = "HotelListingApi";
        private const string _refreshToken = "RefreshToken";

        public AuthManager(IMapper mapper, UserManager<ApiUser> userManager, IConfiguration configuration,
            ILogger<AuthManager> logger) 
        {
            this._mapper = mapper;
            this._userManager = userManager;
            this._configuration = configuration;
            this._logger = logger;
        }

        public async Task<string> CreateRefreshToken()
        {
            await _userManager.RemoveAuthenticationTokenAsync(
                _user, _loginProvider, _refreshToken);  // remove old Token
            
            var newRefreshToken = await _userManager.GenerateUserTokenAsync(
                _user, _loginProvider, _refreshToken);  // new Token

            var result = await _userManager.SetAuthenticationTokenAsync(
                _user, _loginProvider, _refreshToken, newRefreshToken);
            return newRefreshToken;
        }

        public async Task<AuthResponseDto> Login(LoginDto loginDto)
        {
            _logger.LogInformation($"Looking for user with email {loginDto.Email}");
            _user = await _userManager.FindByEmailAsync(loginDto.Email);  // find user in DB
            bool isValidUser = await _userManager.CheckPasswordAsync(_user, loginDto.Password); // check password Dto vs DB

            if (_user is null ||  !isValidUser) 
            {
                _logger.LogWarning($"User with email {loginDto.Email} was not found");
                return null;
            }
            var token = await GenerateToken();
            _logger.LogInformation($"Token generated for user with email {loginDto.Email} | Token: {token}");
            
            return new AuthResponseDto
            {
                Token = token,
                UserId = _user.Id,
                RefreshToken = await CreateRefreshToken(),
            };
        }

        public async Task<IEnumerable<IdentityError>> Register(ApiUserDto userDto)
        {
            _user = _mapper.Map<ApiUser>(userDto);
            _user.UserName = userDto.Email;  //username for login/register will be email

            var result = await _userManager.CreateAsync(_user, userDto.Password);  // encrypted
            
            if (result.Succeeded) 
            {
                await _userManager.AddToRoleAsync(_user, "User");  // assign role "User"
            }
            return result.Errors;  // [] or [Errors]
        }

        public async Task<AuthResponseDto> VerifyRefreshToken(AuthResponseDto request)
        {
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            var tokenContent = jwtSecurityTokenHandler.ReadJwtToken(request.Token);
            var username = tokenContent.Claims.ToList().FirstOrDefault(q => q.Type ==
            JwtRegisteredClaimNames.Email)?.Value;  // not Exception but return null
            _user = await _userManager.FindByNameAsync(username);  // we use Email as username in Token

            if (_user == null || _user.Id != request.UserId)  // "|| _user.Id != request.UserId" - optional?!
            {
                return null;
            }

            var isValidRefreshToken = await _userManager.VerifyUserTokenAsync(_user, _loginProvider,
                _refreshToken, request.RefreshToken);  // vs AuthResponseDto.RefreshToken

            if (isValidRefreshToken) 
            {
                var token = await GenerateToken();
                return new AuthResponseDto
                {
                    Token = token,
                    UserId = _user.Id,
                    RefreshToken = await CreateRefreshToken()
                };
            }
            await _userManager.UpdateSecurityStampAsync(_user);
            return null;
        }

        private async Task<string> GenerateToken()
        {
            var securitykey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["JwtSettings:Key"]));

            var credentials = new SigningCredentials(securitykey, SecurityAlgorithms.HmacSha256);

            var roles = await _userManager.GetRolesAsync(_user);  // get roles for user
            var roleClaims = roles.Select(x => new Claim(ClaimTypes.Role, x)).ToList();  //claims for roles
            var userClaims = await _userManager.GetClaimsAsync(_user);  //in case we configure claims on user

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, _user.Email),  //Sub - subject token is issued to
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),  //change token every call
                new Claim(JwtRegisteredClaimNames.Email, _user.Email),
                new Claim("uid", _user.Id),  // our custom claim
            }
            .Union(userClaims).Union(roleClaims);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],  // from appsettings.json
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToInt32(_configuration["JwtSettings:DurationInMinutes"])),
                signingCredentials: credentials
                );
            
            return new JwtSecurityTokenHandler().WriteToken(token);

        }
    }
}
