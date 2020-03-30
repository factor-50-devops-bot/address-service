﻿using AddressService.Core.Dto;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using AddressService.Core.Domains.Entities.Response;
using AddressService.Core.Utils;

namespace AddressService.Mappers
{
    public class QasMapper : IQasMapper
    {
        public PostcodeDto MapToPostcodeDto(string postcode, IEnumerable<QasFormatRootResponse> qasFormatRootResponses)
        {
            postcode = PostcodeCleaner.CleanPostcode(postcode);
            PostcodeDto postcodeDto = new PostcodeDto();
            postcodeDto.Postcode = postcode;

            foreach (var qasFormatRootResponse in qasFormatRootResponses)
            {
                foreach (var address in qasFormatRootResponse.Address)
                {
                    address.PostalCode = PostcodeCleaner.CleanPostcode(address.PostalCode);

                    postcodeDto.AddressDetails.Add(new AddressDetailsDto()
                    {
                        AddressLine1 = address.AddressLine1,
                        AddressLine2 = address.AddressLine2,
                        AddressLine3 = address.AddressLine3,
                        Locality = address.Locality,
                        PostalCode = postcode,
                    });

                    if (address.PostalCode != postcode)
                    {
                        throw new Exception("This method should map addresses for a single postcode");
                    }
                }
            }

            return postcodeDto;
        }

        /// <summary>
        /// Returns Format IDs grouped by postcode
        /// </summary>
        /// <param name="qasSearchRootResponses"></param>
        /// <returns></returns>
        public ILookup<string, string> GetFormatIds(IEnumerable<QasSearchRootResponse> qasSearchRootResponses)
        {
            List<Tuple<string, string>> formatIds = new List<Tuple<string, string>>();

            foreach (var qasSearchRootResponse in qasSearchRootResponses)
            {
                foreach (var result in qasSearchRootResponse.Results)
                {
                    var postCodeWithFormatId = new Tuple<string, string>(qasSearchRootResponse.Postcode, GetFormatIdFromUri(result.Format));

                    // todo throw exception if any format ids are null until better error handling has been decided (e.g. ignore any null addresses with the consequence that some addresses won't be stored)
                    if (postCodeWithFormatId.Item2 == null)
                    {
                        throw new Exception("Qas Format Id is null");
                    }

                    formatIds.Add(postCodeWithFormatId);
                }
            }

            ILookup<string, string> formatIdsLookup = formatIds.ToLookup(x => x.Item1, x => x.Item2);

            return formatIdsLookup;
        }

        //public IEnumerable<string> GetFormatIds(IEnumerable<QasSearchRootResponse> qasSearchRootResponses)
        //{
        //    List<string> formatIds = new List<string>();

        //    foreach (var qasSearchRootResponse in qasSearchRootResponses)
        //    {
        //        formatIds.AddRange(GetFormatIds(qasSearchRootResponse));
        //    }

        //    return formatIds;
        //}

        //public IEnumerable<string> GetFormatIds(QasSearchRootResponse qasSearchRootResponse)
        //{
        //    var formatIds = qasSearchRootResponse.Results.Select(x => GetFormatIdFromUri(x.Format));

        //    // throw exception if any format ids are null until better error handling has been decided (e.g. ignore any null addresses with the consequence that some addresses won't be stored)
        //    if (formatIds.Any(x => x == null))
        //    {
        //        throw new Exception("Qas Format Id is null");
        //    }

        //    return formatIds;
        //}

        private string GetFormatIdFromUri(string uri)
        {
            NameValueCollection queryParameters = HttpUtility.ParseQueryString(uri);
            string id = queryParameters.Get("id");
            return id;
        }
    }
}
