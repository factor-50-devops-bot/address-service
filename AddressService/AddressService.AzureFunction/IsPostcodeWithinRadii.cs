﻿using AddressService.Core.Utils;
using AddressService.Core.Validation;
using HelpMyStreet.Contracts.AddressService.Request;
using HelpMyStreet.Contracts.AddressService.Response;
using HelpMyStreet.Contracts.Shared;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;


namespace AddressService.AzureFunction
{
    public class IsPostcodeWithinRadii
    {
        private readonly IMediator _mediator;
        private readonly IPostcodeValidator _postcodeValidator;
        private readonly ILoggerWrapper<IsPostcodeWithinRadii> _logger;

        public IsPostcodeWithinRadii(IMediator mediator, IPostcodeValidator postcodeValidator, ILoggerWrapper<IsPostcodeWithinRadii> logger)
        {
            _mediator = mediator;
            _postcodeValidator = postcodeValidator;
            _logger = logger;
        }

        [FunctionName("IsPostcodeWithinRadii")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(ResponseWrapper<IsPostcodeWithinRadiiResponse, AddressServiceErrorCode>))]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError, Type = typeof(ResponseWrapper<IsPostcodeWithinRadiiResponse, AddressServiceErrorCode>))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage reqAsHttpRequestMessage,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("C# HTTP trigger function processed a request.");

                // accept compressed requests (can't do this with middleware)
                IsPostcodeWithinRadiiRequest req = await HttpRequestMessageCompressionUtils.DeserialiseAsync<IsPostcodeWithinRadiiRequest>(reqAsHttpRequestMessage);

                //This validation logic belongs in a custom validation attribute on the Postcode property.  However, validationContext.GetService<IExternalService> always returned null in the validation attribute (despite DI working fine elsewhere). I didn't want to spend a lot of time finding out why when there is lots to do so I've put the postcode validation logic here for now.
                if (!await _postcodeValidator.IsPostcodeValidAsync(req.Postcode))
                {
                    return new OkObjectResult(ResponseWrapper<IsPostcodeWithinRadiiResponse, AddressServiceErrorCode>.CreateUnsuccessfulResponse(AddressServiceErrorCode.InvalidPostcode, "Invalid postcode"));
                }

                if (req.IsValid(out var validationResults))
                {
                    IsPostcodeWithinRadiiResponse response = await _mediator.Send(req, cancellationToken);

                    return new OkObjectResult(ResponseWrapper<IsPostcodeWithinRadiiResponse, AddressServiceErrorCode>.CreateSuccessfulResponse(response));
                }
                else
                {
                    return new OkObjectResult(ResponseWrapper<IsPostcodeWithinRadiiResponse, AddressServiceErrorCode>.CreateUnsuccessfulResponse(AddressServiceErrorCode.ValidationError, validationResults));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Unhandled error in IsPostcodeWithinRadii", ex);
                return new ObjectResult(ResponseWrapper<IsPostcodeWithinRadiiResponse, AddressServiceErrorCode>.CreateUnsuccessfulResponse(AddressServiceErrorCode.UnhandledError, "Internal Error")) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
    }
}

