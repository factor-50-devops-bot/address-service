﻿using AddressService.Core.Dto;
using AddressService.Core.Interfaces.Repositories;
using AddressService.Repo.EntityFramework.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AddressService.Repo
{
    public class Repository : IRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public Repository(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // todo test GetMissingPostcodes is fast enough without using TVPs
        public async Task<IEnumerable<PostcodeDto>> GetPostcodesAsync(IEnumerable<string> postCodes)
        {
            List<PostcodeEntity> postcodeEntities = await _context.PostCode.Where(x => postCodes.Contains(x.Postcode)).Include(x => x.AddressDetails).ToListAsync();

            IEnumerable<PostcodeDto> missingPostCodes = _mapper.Map<IEnumerable<PostcodeEntity>, IEnumerable<PostcodeDto>>(postcodeEntities);

            return missingPostCodes;
        }

        // todo test SavePostcodes is fast enough without using TVPs (the answer will be no...)
        public async Task SavePostcodesAsync(IEnumerable<PostcodeDto> postCodes)
        {
            IEnumerable<PostcodeEntity> missingPostCodesEntities = _mapper.Map<IEnumerable<PostcodeDto>, IEnumerable<PostcodeEntity>>(postCodes);

            await _context.PostCode.AddRangeAsync(missingPostCodesEntities);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsPostcodeInDb(string postcode)
        {
            var result = await _context.PostCode.AnyAsync(x => x.Postcode == postcode);
            return result;
        }
    }
}
