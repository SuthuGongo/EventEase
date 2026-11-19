namespace EventEaseApp.Services
{
    // Abstraction over Azure Blob Storage so controllers don't depend on
    // the Azure SDK directly, and so this can be mocked/swapped in tests.
    public interface IBlobStorageService
    {
        /// <summary>
        /// Uploads an image file to the given container and returns its public URL.
        /// A new, unique blob name is generated so uploads never collide or overwrite
        /// each other, even if two users pick files with the same original name.
        /// </summary>
        Task<string> UploadImageAsync(IFormFile file, string containerName);

        /// <summary>
        /// Deletes the blob referenced by the given URL from the given container.
        /// Safe to call with a null/empty/non-blob URL (e.g. a legacy placeholder
        /// link) - it will simply do nothing in that case.
        /// </summary>
        Task DeleteImageAsync(string? blobUrl, string containerName);
    }
}
