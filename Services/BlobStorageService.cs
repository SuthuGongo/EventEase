using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace EventEaseApp.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<BlobStorageService> _logger;

        // Basic guardrails so the app never stores something that isn't
        // actually an image, and never accepts an unreasonably large upload.
        private static readonly string[] AllowedContentTypes =
        {
            "image/jpeg", "image/png", "image/gif", "image/webp"
        };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

        public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
        {
            _logger = logger;

            var connectionString = configuration.GetConnectionString("AzureStorageConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Azure Storage connection string 'AzureStorageConnection' is missing from configuration.");
            }

            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task<string> UploadImageAsync(IFormFile file, string containerName)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("No file was provided.");
            }

            if (file.Length > MaxFileSizeBytes)
            {
                throw new ArgumentException("Image must be smaller than 5 MB.");
            }

            if (!AllowedContentTypes.Contains(file.ContentType?.ToLowerInvariant()))
            {
                throw new ArgumentException("Only JPG, PNG, GIF, or WEBP images are allowed.");
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            // Public read access on the blobs only (not container listing), since
            // these images are displayed directly in <img> tags on public pages.
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // Guid-based name: avoids collisions/overwrites and strips any
            // unsafe characters that might have been in the original filename.
            var extension = Path.GetExtension(file.FileName);
            var blobName = $"{Guid.NewGuid()}{extension}";

            var blobClient = containerClient.GetBlobClient(blobName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                });
            }

            _logger.LogInformation("Uploaded image {BlobName} to container {Container}", blobName, containerName);

            return blobClient.Uri.ToString();
        }

        public async Task DeleteImageAsync(string? blobUrl, string containerName)
        {
            if (string.IsNullOrWhiteSpace(blobUrl))
            {
                return;
            }

            try
            {
                var uri = new Uri(blobUrl);
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

                // Only attempt deletion if the URL actually points at this
                // container - protects old seed/placeholder URLs from other
                // hosts (e.g. via.placeholder.com) from causing errors.
                if (!uri.AbsolutePath.Contains($"/{containerName}/", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var blobName = Path.GetFileName(uri.LocalPath);
                var blobClient = containerClient.GetBlobClient(blobName);
                await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                // Deleting the old image is a best-effort cleanup step - if it
                // fails, we don't want that to block the user's actual CRUD action.
                _logger.LogWarning(ex, "Failed to delete blob at {BlobUrl}", blobUrl);
            }
        }
    }
}
