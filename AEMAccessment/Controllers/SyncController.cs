using AEMAccessment.IService;
using AEMAccessment.Models;
using Microsoft.AspNetCore.Mvc;
using System;

namespace AEMAccessment.Controllers
{
    //Author: M. Arsyad - 17/06/2026

    //Assessment PART 2
    //    SELECT
    //    p.UniqueName AS PlatformName,
    //    w.Id,
    //    w.PlatformId,
    //    w.UniqueName,
    //    w.Latitude,
    //    w.Longitude,
    //    w.CreatedAt,
    //    w.UpdatedAt
    //FROM Wells w
    //INNER JOIN Platforms p ON w.PlatformId = p.Id
    //INNER JOIN (
    //    SELECT PlatformId, MAX(UpdatedAt) AS LastUpdatedAt
    //    FROM Wells
    //    GROUP BY PlatformId
    //) latest ON w.PlatformId = latest.PlatformId
    //        AND w.UpdatedAt = latest.LastUpdatedAt
    //ORDER BY p.Id


    [ApiController]
    [Route("api/[controller]")]
    public class SyncController : ControllerBase
    {
        private readonly IApiSyncService _syncService;
        private readonly ILogger<SyncController> _logger;

        public SyncController(IApiSyncService syncService, ILogger<SyncController> logger)
        {
            _syncService = syncService;
            _logger = logger;
        }

        [HttpPost("actual")]
        public async Task<IActionResult> SyncActual()
        {
            try
            {
                await _syncService.SyncPlatformWellActualAsync();
                return Ok(new { success = true, message = "Sync from GetPlatformWellActual completed." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync actual failed.");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("dummy")]
        public async Task<IActionResult> SyncDummy()
        {
            try
            {
                await _syncService.SyncPlatformWellDummyAsync();
                return Ok(new { success = true, message = "Sync from GetPlatformWellDummy completed." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync dummy failed.");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }


        [HttpGet("token")]
        public async Task<IActionResult> GetToken()
        {
            try
            {
                var token = await _syncService.LoginAsync();
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed.");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
