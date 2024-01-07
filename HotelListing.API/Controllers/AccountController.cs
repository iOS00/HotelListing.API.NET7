using HotelListing.API.DTOs.Users;
using HotelListing.API.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelListing.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAuthManager _authManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAuthManager authManager, ILogger<AccountController> logger)
        {
            this._authManager = authManager;
            this._logger = logger;
        }

        // POST: api/Account/register
        [HttpPost]
        [Route("register")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]  //also configures Swagger
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> Register([FromBody] ApiUserDto apiUserDto)  //only from request body
        {
            _logger.LogInformation($"Registration Attempt for {apiUserDto.Email}");  // log Info
            try 
            {
                var errors = await _authManager.Register(apiUserDto);
                if (errors.Any())
                {
                    foreach (var error in errors)
                    {
                        ModelState.AddModelError(error.Code, error.Description);
                    }
                    return BadRequest(ModelState);
                }
                return Ok();
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, $"Something went wrong in the {nameof(Register)} -" +
                    $" User Registration attempt for {apiUserDto.Email}");  //log Error
                return Problem($"Something went wrong in the {nameof(Register)}", statusCode: 500);  //instead of "throw"
            }
            
        }

        // POST: api/Account/login
        [HttpPost]
        [Route("login")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]  //also configures Swagger
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> Login([FromBody] LoginDto loginDto)  //only from request body
        {
            _logger.LogInformation($"Login Attempt for {loginDto.Email}");  // log Info
            
            try
            {
                var authResponse = await _authManager.Login(loginDto);
                if (authResponse is null)
                {
                    return Unauthorized();  // 401 Unauthorized
                }
                return Ok(authResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Something went wrong in the {nameof(Login)}");  //log Error
                return Problem($"Something went wrong in the {nameof(Login)}", statusCode: 500);
            }
            
        }

        // POST: api/Account/refreshtoken
        [HttpPost]
        [Route("refreshtoken")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]  //also configures Swagger
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> RefreshToken([FromBody] AuthResponseDto request)  //only from request body
        {
            var authResponse = await _authManager.VerifyRefreshToken(request);
            if (authResponse is null)
            {
                return Unauthorized();  // 401 Unauthorized
            }
            return Ok(authResponse);
        }
    }
}
