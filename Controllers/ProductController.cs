using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductManagementSystem.DTOs;
using ProductManagementSystem.Services.Interface;

namespace ProductManagementSystem.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IJwtService _jwtService;
        private readonly IValidator<ProductDto> _productValidator;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            IProductService productService,
            IJwtService jwtService,
            IValidator<ProductDto> productValidator,
            ILogger<ProductController> logger)
        {
            _productService  = productService;
            _jwtService      = jwtService;
            _productValidator = productValidator;
            _logger= logger;
        }

        // GET  api/product/getAll
        [HttpPost("getAll")]
        public async Task<IActionResult> GetAll([FromBody] OrderParamDto orderParamDto)
        {
            if (orderParamDto == null)
                return BadRequest("Pagination parameters are required.");
            _logger.LogInformation(
                "GetAll products — page {Page}", orderParamDto.PageNumber);
            var result = await _productService.GetAllAsync(orderParamDto);
            return Ok(result);
        }

        // GET  api/product/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogInformation("GetById product {ProductId}", id); 
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} not found", id);
                return NotFound($"Product with ID {id} was not found.");
            }

            return Ok(product);
        }

        // POST api/product
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductDto productDto)
        {
            if (productDto == null)
                return BadRequest("Product data is required.");

            var validationResult = await _productValidator.ValidateAsync(productDto);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));

            var currentUserId = _jwtService.GetCurrentUser();
            _logger.LogInformation(
               "Creating product '{Name}' by user {UserId}",
               productDto.ProductName, currentUserId);
            var created = await _productService.CreateAsync(productDto, currentUserId);

            return CreatedAtAction(nameof(GetById), new { id = created.ProductId }, created);
        }

        // PUT  api/product/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductDto productDto)
        {
            if (productDto == null)
                return BadRequest("Product data is required.");

            var validationResult = await _productValidator.ValidateAsync(productDto);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));

            try
            {
                var currentUserId = _jwtService.GetCurrentUser();
                _logger.LogInformation(
                   "Updating product {ProductId} by user {UserId}", id, currentUserId);

                var updated = await _productService.UpdateAsync(id, productDto, currentUserId);
                return Ok(updated);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Update — product {ProductId} not found", id);
                return NotFound(ex.Message);
            }
        }

        // DELETE api/product/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                _logger.LogInformation("Deleting product {ProductId}", id);
                await _productService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Delete — product {ProductId} not found", id);
                return NotFound(ex.Message);
            }
        }
    }
}