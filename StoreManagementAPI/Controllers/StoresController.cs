using Microsoft.AspNetCore.Mvc;
using StoreManagementAPI.Services;
using StoreManagementAPI.Models;

namespace StoreManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoresController : ControllerBase
    {
        private readonly IStoreService _storeService;
        private readonly ITenantContext _tenantContext;
        public StoresController(IStoreService storeService, ITenantContext tenantContext)
        {
            _storeService = storeService;
            _tenantContext = tenantContext;
        }

        private bool IsTenantContextValid()
        {
            return _tenantContext.CompanyId.HasValue;
        }

        [HttpGet]
        public async Task<IActionResult> GetStores()
        {
            if (!IsTenantContextValid())
            {
                return BadRequest("X-Company-ID header is required.");
            }
            var stores = await _storeService.GetAllStoresAsync();
            return Ok(stores);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStoreById(Guid id)
        {
            if (!IsTenantContextValid())
            {
                return BadRequest("X-Company-ID header is required.");
            }

            var store = await _storeService.GetStoreByIdAsync(id);
            if (store == null)
            {
                return NotFound();
            }
            return Ok(store);
        }
        [HttpPost]
        public async Task<IActionResult> CreateStore(StoreCreateDto storeDto)
        {
            if (!IsTenantContextValid())
            {
                return BadRequest("X-Company-ID header is required.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var store = new Store
            {
                Name = storeDto.Name,
                Address = storeDto.Address,
                CompanyId = _tenantContext.CompanyId!.Value
            };

            var createdStore = await _storeService.CreateStoreAsync(store);
            return CreatedAtAction(nameof(GetStoreById), new { id = createdStore.Id }, createdStore);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStore(Guid id, StoreCreateDto storeDto) // Changed from StoreCreateDto to handle both create and update
        {
            if (!IsTenantContextValid())
            {
                return BadRequest("X-Company-ID header is required.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var store = new Store
            {
                Id = id,
                Name = storeDto.Name,
                Address = storeDto.Address,
                CompanyId = _tenantContext.CompanyId!.Value
            };

            var updatedStore = await _storeService.UpdateStoreAsync(id, store);
            if (updatedStore == null)
            {
                return NotFound();
            }

            return NoContent();
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStore(Guid id)
        {
            if (!IsTenantContextValid())
            {
                return BadRequest("X-Company-ID header is required.");
            }

            var deleted = await _storeService.DeleteStoreAsync(id);
            if (!deleted)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}