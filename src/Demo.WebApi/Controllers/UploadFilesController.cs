using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demo.WebApi.Controllers;

/// <summary>
/// Controller demonstrating various file upload scenarios
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UploadFilesController : ControllerBase
{
    /// <summary>
    /// Upload a single file using IFormFile
    /// </summary>
    [HttpPost("single")]
    public ActionResult UploadSingle(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        return Ok(new
        {
            fileName = file.FileName,
            size = file.Length,
            contentType = file.ContentType
        });
    }

    /// <summary>
    /// Upload files with additional metadata from various sources
    /// </summary>
    [HttpPost("{projectId}/with-metadata")]
    public ActionResult UploadWithMetadata(
        [FromRoute] int projectId,
        IFormFile file,
        [FromForm] string description,
        [FromForm] string category,
        [FromQuery] string? tags = null,
        [FromQuery] bool includeMetadata = true,
        [FromHeader(Name = "X-Upload-Source")] string? uploadSource = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        return Ok(new
        {
            projectId,
            fileName = file.FileName,
            size = file.Length,
            contentType = file.ContentType,
            description,
            category,
            tags,
            includeMetadata,
            uploadSource = uploadSource ?? "Unknown"
        });
    }

    /// <summary>
    /// Upload files as a list
    /// </summary>
    [HttpPost("list")]
    public ActionResult UploadList(List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest("No files uploaded");

        return Ok(new
        {
            count = files.Count,
            totalSize = files.Sum(f => f.Length),
            files = files.Select(f => new
            {
                fileName = f.FileName,
                size = f.Length
            })
        });
    }

    /// <summary>
    /// Upload files as an array
    /// </summary>
    [HttpPost("array")]
    public ActionResult UploadArray(IFormFile[] files)
    {
        if (files == null || files.Length == 0)
            return BadRequest("No files uploaded");

        return Ok(new
        {
            count = files.Length,
            totalSize = files.Sum(f => f.Length),
            files = files.Select(f => new
            {
                fileName = f.FileName,
                size = f.Length
            })
        });
    }

    /// <summary>
    /// Complex upload demonstrating all parameter sources
    /// </summary>
    [HttpPost("projects/{projectId}/documents/{documentId}")]
    public ActionResult ComplexUpload(
        [FromRoute] int projectId,
        [FromRoute] string documentId,
        IFormFile file,
        [FromForm] string title,
        [FromForm] string description,
        [FromQuery] string? version = null,
        [FromQuery] bool overwrite = false,
        [FromQuery] string? tags = null,
        [FromHeader(Name = "X-Api-Key")] string? apiKey = null,
        [FromHeader(Name = "X-User-Agent")] string? userAgent = null,
        [FromForm] string? author = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        if (string.IsNullOrEmpty(title))
            return BadRequest("Title is required");

        return Ok(new
        {
            // Route parameters
            projectId,
            documentId,
            
            // Query parameters
            version = version ?? "1.0",
            overwrite,
            tags,
            
            // Header parameters
            apiKey = string.IsNullOrEmpty(apiKey) ? "Not provided" : "***" + apiKey[^4..],
            userAgent = userAgent ?? "Unknown",
            
            // File
            file = new
            {
                fileName = file.FileName,
                size = file.Length,
                contentType = file.ContentType
            },
            
            // Form data
            title,
            description,
            author = author ?? "Anonymous",
            
            uploadedAt = DateTime.UtcNow
        });
    }
}
