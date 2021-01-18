using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Api.ApiResponses;
using Api.Controllers;
using Api.Dtos;
using Api.Extensions;
using Api.Helpers;
using AutoMapper;
using Core.CommandHandlers;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Core.Specifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [ApiExplorerSettings(GroupName = "Orders")]
    public class OrdersController : BaseApiController
    {
        private readonly IGenericRepositoryResolver _repoResolver;
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;

        public OrdersController(IGenericRepositoryResolver repoResolver, IMapper mapper, IMediator mediator)
        {
            _repoResolver = repoResolver;
            _mapper = mapper;
            _mediator = mediator;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Order>> CreateOrder(OrderDto orderDto)
        {
            var email = HttpContext.User.RetrieveEmailFromPrincipal();

            var address = _mapper.Map<AddressDto, Address>(orderDto.ShipToAddress);

            var order = await _mediator.Send(new CreateOrderRequest(email, orderDto.DeliveryMethodId, orderDto.BasketId, address));

            if (order == null)
            {
                return BadRequest(new ApiResponse(HttpStatusCode.BadRequest, "Problem creating order"));
            }

            return Created("orders", order);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetOrdersForUser()
        {
            var email = HttpContext.User.RetrieveEmailFromPrincipal();
            var spec = new OrdersWithItemsAndOrderingSpecification(email);

            var orders = await _repoResolver.Repository<Order>().ListAsync(spec);

            return Ok(_mapper.Map<IReadOnlyList<Order>, IReadOnlyList<OrderToReturnDto>>(orders));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderToReturnDto>> GetOrderByIdForUser(int id)
        {
            var email = HttpContext.User.RetrieveEmailFromPrincipal();
            var spec = new OrdersWithItemsAndOrderingSpecification(id, email);

            var order = await _repoResolver.Repository<Order>().GetEntityWithSpec(spec);

            if (order == null)
            {
                return NotFound(new ApiResponse(HttpStatusCode.NotFound));
            }

            return _mapper.Map<Order, OrderToReturnDto>(order);
        }

        [HttpGet("deliveryMethods")]
        [Cached(300)]
        [AllowAnonymous]
        public async Task<ActionResult<IReadOnlyList<DeliveryMethod>>> GetDeliveryMethods()
        {
            return Ok(await _repoResolver.Repository<DeliveryMethod>().ListAllAsync());
        }
    }
}