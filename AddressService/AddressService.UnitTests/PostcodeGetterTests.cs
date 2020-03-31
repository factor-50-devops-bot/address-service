﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AddressService.Core.Dto;
using AddressService.Core.Interfaces.Repositories;
using AddressService.Handlers;
using AddressService.Handlers.Qas;
using AddressService.Mappers;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace AddressService.UnitTests
{

    public class PostcodeGetterTests
    {
        private Mock<IRepository> _repository;
        private Mock<IQasService> _qasService;
        private Mock<IQasMapper> _qasMapper;

        private PostcodeDto _missingPostcodeDtosFromQas;
        private IEnumerable<PostcodeDto> _postcodeDtosInDbs;

        [SetUp]
        public void SetUp()
        {
            _repository = new Mock<IRepository>();

            _postcodeDtosInDbs = new List<PostcodeDto>()
            {
                new PostcodeDto()
                {
                    Id = 1,
                    Postcode = "NG15FS",
                    AddressDetails = new List<AddressDetailsDto>()
                    {
                        new AddressDetailsDto()
                        {
                            AddressLine1 = "1_addressline1",
                            AddressLine2 = "1_addressline2",
                            AddressLine3 = "1_addressline1",
                            Locality = "1_locality"
                        }
                    }
                }
            };
            _repository.Setup(x => x.GetPostcodesAsync(It.Is<IEnumerable<string>>(y => !y.Contains("NG15FS")))).ReturnsAsync(new List<PostcodeDto>());
            _repository.Setup(x => x.GetPostcodesAsync(It.Is<IEnumerable<string>>(y => y.Contains("NG15FS")))).ReturnsAsync(_postcodeDtosInDbs);

            _repository.SetupAllProperties();


            _qasService = new Mock<IQasService>();

            _qasService.SetupAllProperties();

            _qasMapper = new Mock<IQasMapper>();

            ILookup<string, string> missingQasFormatIdsGroupedByPostCode = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("ng15ba", "AC4705C7-E358-4338-A4FC-DE5EA241B5F1")
            }.ToLookup(x => x.Key, x => x.Value);

            _qasMapper.Setup(x => x.GetFormatIds(It.IsAny<IEnumerable<QasSearchRootResponse>>())).Returns(missingQasFormatIdsGroupedByPostCode);

            _missingPostcodeDtosFromQas = new PostcodeDto()
            {
                Postcode = "NG16DQ",
                AddressDetails = new List<AddressDetailsDto>()
                {
                    new AddressDetailsDto()
                    {
                        AddressLine1 = "2_addressline1",
                        AddressLine2 = "2_addressline2",
                        AddressLine3 = "2_addressline1",
                        Locality = "2_locality"
                    }
                }
            };

            _qasMapper.Setup(x => x.MapToPostcodeDto(It.IsAny<string>(), It.IsAny<IEnumerable<QasFormatRootResponse>>())).Returns(_missingPostcodeDtosFromQas);
        }

        [Test]
        public async Task PostCodesAreRetrievedFromQasAndDatabase()
        {
            CancellationToken cancellationToken = new CancellationToken();
            PostcodeGetter postcodeGetter = new PostcodeGetter(_repository.Object, _qasService.Object, _qasMapper.Object);

            List<string> postcodes = new List<string>()
            {
                "NG1 6DQ",
                "NG1 5FS"
            };

            IEnumerable<PostcodeDto> result = await postcodeGetter.GetPostcodesAsync(postcodes, cancellationToken);

            _repository.Verify(x => x.GetPostcodesAsync(It.IsAny<IEnumerable<string>>()), Times.Once);

            _repository.Verify(x => x.SavePostcodesAsync(It.IsAny<IEnumerable<PostcodeDto>>()), Times.Once);

            _qasMapper.Verify(x => x.GetFormatIds(It.IsAny<IEnumerable<QasSearchRootResponse>>()), Times.Once);
            _qasMapper.Verify(x => x.MapToPostcodeDto(It.IsAny<string>(), It.IsAny<IEnumerable<QasFormatRootResponse>>()), Times.Once);

            _qasService.Verify(x => x.GetGlobalIntuitiveSearchResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _qasService.Verify(x => x.GetGlobalIntuitiveFormatResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);


            Assert.AreEqual(2, result.Count());

            List<AddressDetailsDto> returnedMissingPostCodeAddress = result.FirstOrDefault(x => x.Postcode == "NG16DQ").AddressDetails;
            List<AddressDetailsDto> returnedPostCodeInDbAddress = result.FirstOrDefault(x => x.Postcode == "NG15FS").AddressDetails;

            Assert.AreEqual(1, returnedMissingPostCodeAddress.Count());
            Assert.AreEqual(1, returnedPostCodeInDbAddress.Count());

        }

        [Test]
        public async Task MissingPostCodeIsRetrievedFromQas()
        {
            CancellationToken cancellationToken = new CancellationToken();
            PostcodeGetter postcodeGetter = new PostcodeGetter(_repository.Object, _qasService.Object, _qasMapper.Object);
            
            PostcodeDto result = await postcodeGetter.GetPostcodeAsync("ng1 6dq", cancellationToken);

            _repository.Verify(x => x.GetPostcodesAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
            _repository.Verify(x => x.SavePostcodesAsync(It.IsAny<IEnumerable<PostcodeDto>>()), Times.Once);

            _qasMapper.Verify(x => x.GetFormatIds(It.IsAny<IEnumerable<QasSearchRootResponse>>()), Times.Once);
            _qasMapper.Verify(x => x.MapToPostcodeDto(It.IsAny<string>(), It.IsAny<IEnumerable<QasFormatRootResponse>>()), Times.Once);

            _qasService.Verify(x => x.GetGlobalIntuitiveSearchResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _qasService.Verify(x => x.GetGlobalIntuitiveFormatResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

            Assert.AreEqual("NG16DQ", result.Postcode);
        }

        [Test]
        public async Task PostCodeIsRetrievedFromDatabase()
        {
            CancellationToken cancellationToken = new CancellationToken();
            PostcodeGetter postcodeGetter = new PostcodeGetter(_repository.Object, _qasService.Object, _qasMapper.Object);

            PostcodeDto result = await postcodeGetter.GetPostcodeAsync("ng1 5fs", cancellationToken);

            _repository.Verify(x => x.GetPostcodesAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
            _repository.Verify(x => x.SavePostcodesAsync(It.IsAny<IEnumerable<PostcodeDto>>()), Times.Never);

            _qasMapper.Verify(x => x.GetFormatIds(It.IsAny<IEnumerable<QasSearchRootResponse>>()), Times.Never);
            _qasMapper.Verify(x => x.MapToPostcodeDto(It.IsAny<string>(), It.IsAny<IEnumerable<QasFormatRootResponse>>()), Times.Never);

            _qasService.Verify(x => x.GetGlobalIntuitiveSearchResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _qasService.Verify(x => x.GetGlobalIntuitiveFormatResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

            Assert.AreEqual("NG15FS", result.Postcode);
        }
    }
}
