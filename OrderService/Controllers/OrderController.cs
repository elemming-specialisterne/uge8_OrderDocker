using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Dto;
using OrderService.Interfaces;
using OrderService.Models;

namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController(IOrderRepository orderRepository, IOrderItemRepository productOrderRepository, IUserRepository userRepository, IMapper mapper, IHttpClientFactory? httpClientFactory = null) : Controller
    {
        private readonly IOrderRepository _orderRepository = orderRepository;
        private readonly IOrderItemRepository _productOrderRepository = productOrderRepository;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IMapper _mapper = mapper;
        private readonly IHttpClientFactory? _httpClientFactory = httpClientFactory;
        private static readonly HttpClient _defaultClient = new();

        [HttpGet]
        //[Authorize(Roles = "Admin")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<Order>))]
        public IActionResult GetOrders()
        {
            var orders = _mapper.Map<List<OrderDto>>(_orderRepository.GetOrders());

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(orders);
        }

        [HttpGet("user/{userID}")]
        //[Authorize(Roles = "Admin, User")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<Order>))]
        [ProducesResponseType(422)]
        public IActionResult GetOrders(int userID)
        {
            var orders = _mapper.Map<List<OrderDto>>(_orderRepository.GetOrders(userID));

            if (!_userRepository.GetUsers().Any(u => u.Userid == userID))
            {
                ModelState.AddModelError("", "User does not exist");
                return StatusCode(422, ModelState);
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(orders);
        }

        [HttpGet("{orderID}")]
        //[Authorize(Roles = "Admin, User")]
        [ProducesResponseType(200, Type = typeof(OrderDto))]
        public IActionResult GetOrder(int orderID)
        {
            var order = _mapper.Map<OrderDto>(_orderRepository.GetOrder(orderID));

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(order);
        }

        [HttpGet("between")]
        //[Authorize(Roles = "Admin")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<OrderDto>))]
        public IActionResult GetOrdersBetween([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var orders =  _mapper.Map<List<OrderDto>>(_orderRepository.GetOrdersBetween(startDate, endDate));

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(orders);
        }

        [HttpPost("[action]")]
        //[Authorize(Roles = "Admin, User")]
        [ProducesResponseType(204)]
        [ProducesResponseType(422)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateOrder([FromBody] OrderDto orderCreate)
        {
            if (orderCreate == null)
                return BadRequest(ModelState);

            var order = _orderRepository.GetOrders()
                .Where(o => o.Orderid == orderCreate.Orderid)
                .FirstOrDefault();
            if (order is not null)
            {
                ModelState.AddModelError("", "Order Already Exists");
                return StatusCode(422, ModelState);
            }
            if (orderCreate.Orderid != 0)
            {
                ModelState.AddModelError("", "OrderId should be 0 to auto-update");
                return StatusCode(422, ModelState);
            }
            if (!_userRepository.GetUsers().Any(u => u.Userid == orderCreate.Userid))
            {
                ModelState.AddModelError("", "User does not exist");
                return StatusCode(422, ModelState);
            }
            Console.WriteLine("Before api call");
            ICollection<Product> products = await GetProductsAsync();
            foreach (var productOrder in orderCreate.OrderItems)
            {
                if (!products.Any(p => p.Productid == productOrder.Productid))
                {
                    ModelState.AddModelError("", $"Productid {productOrder.Productid} does not exist");
                    return StatusCode(422, ModelState);
                }
                //OrderItems.Add(_mapper.Map<OrderItem>(productOrder));
            }
            Console.WriteLine("after api call");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);


            ICollection<OrderItem> OrderItems = [];
            var orderMap = _mapper.Map<Order>(orderCreate);
            foreach (var productOrder in orderCreate.OrderItems)
            {
                var orderItem = _mapper.Map<OrderItem>(productOrder);
                orderItem.Order = orderMap;
                OrderItems.Add(orderItem);
            }
            orderMap.OrderItems = OrderItems;

            // foreach (var productOrderCreate in orderCreate.OrderItems)
            // {
            //     if (!_productOrderRepository.CreateProductOrder(_mapper.Map<OrderItem>(productOrderCreate)))
            //     {
            //         ModelState.AddModelError("", "Something went wrong while saving");
            //         return StatusCode(500, ModelState);
            //     }
            // }

            if (!_orderRepository.CreateOrder(orderMap))
            {
                ModelState.AddModelError("", "Something went wrong while saving");
                return StatusCode(500, ModelState);
            }

            return NoContent();
        }
        
        protected virtual async Task<ICollection<Product>> GetProductsAsync()
        {
            var client = _httpClientFactory?.CreateClient() ?? _defaultClient;
            var products = new List<Product>();
            
            try 
            {
                HttpResponseMessage response = await client.GetAsync("http://productservice:7125/api/Products");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ICollection<Product>>();
                    if (result != null)
                    {
                        products.AddRange(result);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error and return empty list
                Console.WriteLine($"Error getting products: {ex.Message}");
            }
            
            return products;
        }
    }
}