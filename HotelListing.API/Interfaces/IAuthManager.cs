using HotelListing.API.DTOs.Users;
using Microsoft.AspNetCore.Identity;

namespace HotelListing.API.Interfaces
{
    public interface IAuthManager
    {
        Task<IEnumerable<IdentityError>> Register(ApiUserDto userDto);
        Task<AuthResponseDto> Login(LoginDto loginDto);
        Task<string> CreateRefreshToken();
        Task<AuthResponseDto> VerifyRefreshToken(AuthResponseDto request);
    }
}
