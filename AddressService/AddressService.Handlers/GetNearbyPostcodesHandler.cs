﻿using AddressService.Core.Dto;
using AddressService.Core.Utils;
using AutoMapper;
using HelpMyStreet.Contracts.AddressService.Request;
using HelpMyStreet.Contracts.AddressService.Response;
using HelpMyStreet.Utils.Utils;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AddressService.Handlers
{
    public class GetNearbyPostcodesHandler : IRequestHandler<GetNearbyPostcodesRequest, GetNearbyPostcodesResponse>
    {
        private readonly INearestPostcodeGetter _nearestPostcodeGetter;
        private readonly IMapper _mapper;
        private readonly IPostcodeGetter _postcodeGetter;
        private readonly IAddressDetailsSorter _addressDetailsSorter;

        public GetNearbyPostcodesHandler(INearestPostcodeGetter nearestPostcodeGetter, IMapper mapper, IPostcodeGetter postcodeGetter, IAddressDetailsSorter addressDetailsSorter)
        {
            _nearestPostcodeGetter = nearestPostcodeGetter;
            _mapper = mapper;
            _postcodeGetter = postcodeGetter;
            _addressDetailsSorter = addressDetailsSorter;
        }

        public async Task<GetNearbyPostcodesResponse> Handle(GetNearbyPostcodesRequest request, CancellationToken cancellationToken)
        {
            string postcode = PostcodeFormatter.FormatPostcode(request.Postcode);

            // get nearest postcodes
            IReadOnlyList<NearestPostcodeDto> nearestPostcodeDtos = await _nearestPostcodeGetter.GetNearestPostcodesAsync(postcode, null, null);

            IEnumerable<string> nearestPostcodes = nearestPostcodeDtos.Select(x => x.Postcode).ToList();

            // get postcodes
            IEnumerable<PostcodeDto> postcodeDtos = await _postcodeGetter.GetPostcodesAsync(nearestPostcodes, cancellationToken);

            // create response
            GetNearbyPostcodesResponse getNearbyPostcodesResponse = new GetNearbyPostcodesResponse();

            IEnumerable<GetNearbyPostCodeResponse> getNearbyPostCodeResponses = _mapper.Map<IEnumerable<PostcodeDto>, IEnumerable<GetNearbyPostCodeResponse>>(postcodeDtos);

            getNearbyPostcodesResponse.Postcodes =
                (from getNearbyPostCodeResponse in getNearbyPostCodeResponses
                 join nearestPostcodeDto in nearestPostcodeDtos
                     on getNearbyPostCodeResponse.Postcode equals nearestPostcodeDto.Postcode
                 select new GetNearbyPostCodeResponse
                 {
                     Postcode = getNearbyPostCodeResponse.Postcode,
                     AddressDetails = _addressDetailsSorter.OrderAddressDetailsResponse(getNearbyPostCodeResponse.AddressDetails),
                     DistanceInMetres = nearestPostcodeDto.DistanceInMetres,
                     FriendlyName = getNearbyPostCodeResponse.FriendlyName
                 })
                .OrderBy(x => x.DistanceInMetres)
                .ToList();


            return getNearbyPostcodesResponse;
        }
    }
}
