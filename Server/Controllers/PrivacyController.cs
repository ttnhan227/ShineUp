using Microsoft.AspNetCore.Mvc;
using Server.Interfaces;
using Server.Models;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrivacyController : ControllerBase
{
    private readonly IPrivacyRepository _privacyRepository;
    private readonly ILogger<PrivacyController> _logger;

    public PrivacyController(IPrivacyRepository privacyRepository, ILogger<PrivacyController> logger)
    {
        _privacyRepository = privacyRepository;
        _logger = logger;
    }

    // GET: api/privacy
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Privacy>>> GetPrivacySettings()
    {
        try
        {
            var privacySettings = await _privacyRepository.GetAllAsync();
            return Ok(privacySettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all privacy settings");
            return StatusCode(500, "Internal server error");
        }
    }
} 