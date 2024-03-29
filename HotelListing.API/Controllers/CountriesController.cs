﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelListing.API.Data;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using HotelListing.API.Core.Interfaces;
using HotelListing.API.Core.DTOs.Country;
using HotelListing.API.Core.DTOs;
using HotelListing.API.Core.Exceptions;

namespace HotelListing.API.Controllers
{
    [Route("api/v{version:apiVersion}/countries")]  // with API Version in endpoint name
    [ApiController]
    [ApiVersion("1.0", Deprecated = true)]  // set version as Deprecated
    public class CountriesController : ControllerBase
    {
        private readonly ICountriesRepository _countriesRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CountriesController> _logger;

        public CountriesController(ICountriesRepository countriesRepository, IMapper mapper, 
            ILogger<CountriesController> logger)
        {
            this._countriesRepository = countriesRepository;
            this._mapper = mapper;
            this._logger = logger;
        }

        // GET: api/Countries/GetAll
        [HttpGet("GetAll")]
        public async Task<ActionResult<IEnumerable<GetCountryDto>>> GetCountries()
        {
            var countries = await _countriesRepository.GetAllAsync<GetCountryDto>();
            return Ok(countries);  // return above with "OK" response
        }

        // GET: api/v1/Countries/?StartIndex=0&PageSize=25&PageNumber=1
        [HttpGet]
        public async Task<ActionResult<PagedResult<GetCountryDto>>> GetPagedCountries(
            [FromQuery] QueryParameters queryParameters)  // Queriable
        {
            var pagedCountriesResult = await _countriesRepository.GetAllAsync<GetCountryDto>(queryParameters);
            return Ok(pagedCountriesResult); 
        }

        // GET: api/Countries/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CountryDto>> GetCountry(int id)
        {
            var country = await _countriesRepository.GetDetails(id);
            return Ok(country);
        }

        // PUT: api/Countries/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutCountry(int id, UpdateCountryDto updateCountryDto)  // replace current with country
        {
            if (id != updateCountryDto.Id)
            {
                return BadRequest("Invalid Record Id");  //response 400 with custom message
            }

            try
            {
                await _countriesRepository.UpdateAsync(id, updateCountryDto);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CountryExists(id))
                {
                    return NotFound();  //response 404
                }
                else
                {
                    throw;
                }
            }

            return NoContent();  //response 204
        }

        // POST: api/Countries
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<CountryDto>> PostCountry(CreateCountryDto createCountryDto)
        {
            var country = await _countriesRepository
                .AddAsync<CreateCountryDto, GetCountryDto>(createCountryDto);

            // responsible for URI to access object: /api/Countries/5 
            return CreatedAtAction(nameof(GetCountry), new { id = country.Id }, country);   
        }

        // DELETE: api/Countries/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator,User")]  // multiple roles access level
        public async Task<IActionResult> DeleteCountry(int id)
        {
            await _countriesRepository.DeleteAsync(id);

            return NoContent();
        }

        private async Task<bool> CountryExists(int id)
        {
            return await _countriesRepository.Exists(id);
        }
    }
}