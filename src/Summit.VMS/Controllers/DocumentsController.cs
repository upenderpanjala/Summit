using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Summit.VMS.Authorization;
using Summit.VMS.Services.Interfaces;

namespace Summit.VMS.Controllers;

/// <summary>
/// Victim document management. Uploading/deleting requires ManageVictims
/// (Administrator, Investigator). Downloading requires ViewVictims so the
/// police hierarchy and the Home Minister can read attachments too.
/// </summary>
[Authorize]
public class DocumentsController : Controller
{
    private readonly IDocumentService _docs;
    private readonly IConfiguration _config;

    public DocumentsController(IDocumentService docs, IConfiguration config)
    {
        _docs = docs;
        _config = config;
    }

    private static readonly string[] AllowedExtensions =
        { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".txt", ".xlsx", ".csv" };

    [HttpPost, Authorize(Policy = Policies.ManageVictims), ValidateAntiForgeryToken]
    [RequestSizeLimit(10_485_760)]
    public async Task<IActionResult> Upload(int victimId, IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Toast"] = "Please choose a file to upload.";
            return RedirectToAction("Details", "Victims", new { id = victimId });
        }

        var max = _config.GetValue<long?>("Storage:MaxUploadBytes") ?? 10_485_760;
        if (file.Length > max)
        {
            TempData["Toast"] = "File exceeds the maximum allowed size.";
            return RedirectToAction("Details", "Victims", new { id = victimId });
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            TempData["Toast"] = $"File type '{ext}' is not permitted.";
            return RedirectToAction("Details", "Victims", new { id = victimId });
        }

        await using var stream = file.OpenReadStream();
        await _docs.SaveAsync(victimId, file.FileName, file.ContentType, stream);
        TempData["Toast"] = "Document uploaded.";
        return RedirectToAction("Details", "Victims", new { id = victimId });
    }

    [HttpGet, Authorize(Policy = Policies.ViewVictims)]
    public async Task<IActionResult> Download(int id)
    {
        var result = await _docs.OpenAsync(id);
        if (result is null) return NotFound();
        var (meta, content) = result.Value;
        return File(content, meta.ContentType ?? "application/octet-stream", meta.FileName);
    }

    [HttpPost, Authorize(Policy = Policies.ManageVictims), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int victimId)
    {
        await _docs.DeleteAsync(id);
        TempData["Toast"] = "Document deleted.";
        return RedirectToAction("Details", "Victims", new { id = victimId });
    }
}
