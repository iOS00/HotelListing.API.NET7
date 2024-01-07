using AutoMapper;
using HotelListing.API.Data;
using HotelListing.API.DTOs.Country;
using HotelListing.API.DTOs.Hotel;
using HotelListing.API.DTOs.Users;

namespace HotelListing.API.Configurations
{
    public class MapperConfig : Profile
    {
        public MapperConfig() 
        {
            CreateMap<Country, CreateCountryDto>().ReverseMap();  //allows mapping in both directions
            CreateMap<Country, GetCountryDto>().ReverseMap();
            CreateMap<Country, CountryDto>().ReverseMap();
            CreateMap<Country, UpdateCountryDto>().ReverseMap();
            
            CreateMap<Hotel, HotelDto>().ReverseMap();
            CreateMap<Hotel, CreateHotelDto>().ReverseMap();

            CreateMap<ApiUserDto, ApiUser>().ReverseMap();
        }
    }
}