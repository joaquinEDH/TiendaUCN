
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda.src.Application.DTOs.Product;
using Tienda.src.Application.Services.Interfaces;

namespace Tienda.src.Api.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _svc;
        public ProductsController(IProductService svc) { _svc = svc; }

        // ---------- Público ----------
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var result = await _svc.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _svc.GetByIdAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Producto no encontrado." });
            }
        }

        // ---------- Admin (R80, R83, R87) ----------
        // POST /api/admin/products
        [HttpPost("/api/admin/products")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateProductDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Datos inválidos", errors = ModelState });

            var idStr = await _svc.CreateAsync(dto);
            var id = int.Parse(idStr); // la ruta admin espera {id:int}

            return CreatedAtAction(nameof(GetByIdAdmin), new { id }, new { id });
        }

        // GET /api/admin/products/{id}
        [HttpGet("/api/admin/products/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetByIdAdmin(int id)
        {
            try
            {
                var result = await _svc.GetByIdForAdminAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Producto no encontrado." });
            }
        }

        // GET /api/admin/products
        [HttpGet("/api/admin/products")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllForAdmin()
        {
            var result = await _svc.GetAllAsync(); // Por ahora es lo mismo que el público
            return Ok(result);
        }
        // POST /api/admin/products/{id}/images
        [HttpPost("/api/admin/products/{id:int}/images")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadImage(int id, [FromForm] IFormFile file)
        {
            if (file is null || file.Length == 0)
                return BadRequest(new { message = "Debe adjuntar el archivo en form-data con key 'file'." });

            var ok = await _svc.UploadImageAsync(id, file); // ver método abajo
            if (!ok) return Conflict(new { message = "La imagen ya existe o no se pudo guardar." });

            return Created($"/api/admin/products/{id}/images", new { message = "Imagen subida correctamente." });
        }
    }
}