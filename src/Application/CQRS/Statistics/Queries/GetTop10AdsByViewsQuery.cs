﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Extensions;
using Application.CQRS.Ads.Models;
using Application.Persistence.Interfaces;
using AutoMapper;
using MediatR;

namespace Application.CQRS.Statistics.Queries
{
    public class GetTop10AdsByViewsQuery : IRequest<List<AdDto>>
    {
        #region Classes

        public class Handler : IRequestHandler<GetTop10AdsByViewsQuery, List<AdDto>>
        {
            #region Fields

            private readonly IAdvertoDbContext _context;
            private readonly IMapper _mapper;

            #endregion

            #region Constructors

            public Handler(IAdvertoDbContext context, IMapper mapper)
            {
                _context = context;
                _mapper = mapper;
            }

            #endregion
            
            #region IRequestHandler<GetTop10AdsByViewsQuery, List<AdDto>>

            public async Task<List<AdDto>> Handle(GetTop10AdsByViewsQuery request, CancellationToken cancellationToken)
            {
                return await _context.Ads
                    .Select(ad => new {Ad = ad, AdViewesCount = ad.AdViews.Count()})
                    .OrderByDescending(x => x.AdViewesCount)
                    .Take(10)
                    .Select(x => x.Ad)
                    .ProjectToListAsync<AdDto>(_mapper.ConfigurationProvider, cancellationToken)
                    .ConfigureAwait(false);
            }

            #endregion
        }

        #endregion
    }
}