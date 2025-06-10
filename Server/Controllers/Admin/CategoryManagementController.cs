using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.DTOs.Admin;
using Server.Interfaces.Admin;

namespace Server.Controllers.Admin;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Roles = "Admin")]
public class CategoryManagementController : ControllerBase
{
    private readonly ICategoryManagementRepository _repository;

    public CategoryManagementController(ICategoryManagementRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminCategoryDTO>>> GetCategories()
    {
        var categories = await _repository.GetAllCategoriesAsync();
        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AdminCategoryDTO>> GetCategory(int id)
    {
        var category = await _repository.GetCategoryByIdAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        return Ok(category);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateAdminCategoryDTO categoryDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _repository.CreateCategoryAsync(categoryDto);
        if (!result)
        {
            return BadRequest("Could not create category");
        }

        return Ok();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CreateAdminCategoryDTO categoryDto)
    {
        if (string.IsNullOrWhiteSpace(categoryDto.CategoryName))
        {
            return BadRequest("Category name is required");
        }

        var result = await _repository.UpdateCategoryAsync(id, categoryDto);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    // Status toggle removed as Category model doesn't support it
}