﻿using AddressService.Core.Domains.Entities.Request;
using AddressService.Core.Domains.Entities.Response;
using AddressService.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AddressService.Handlers
{
    public class PostDecrementChampionCountHandler : IRequestHandler<PostDecrementChampionCountRequest>
    {
        private readonly IRepository _repository;

        public PostDecrementChampionCountHandler(IRepository repository)
        {
            _repository = repository;
        }

        public Task<Unit> Handle(PostDecrementChampionCountRequest request, CancellationToken cancellationToken)
        {
            _repository.DecrementChampionCount(request.PostCode);
            return Unit.Task;
        }
    }
}
