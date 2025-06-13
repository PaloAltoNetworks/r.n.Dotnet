using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography;

namespace rnDotnet.Services.Assembly
{
    public class FileHasher : IFileHasher
    {
        private readonly ILogger<FileHasher> _logger;

        public FileHasher(ILogger<FileHasher> logger)
        {
            _logger = logger;
        }

        public string CalculateSHA256(string filePath)
        {
            _logger.LogDebug("Calculating SHA256 for file: {FilePath}", filePath);
            using (SHA256 sha256 = SHA256.Create())
            {
                using (FileStream fileStream = File.OpenRead(filePath))
                {
                    byte[] hashBytes = sha256.ComputeHash(fileStream);
                    string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                    _logger.LogDebug("Calculated SHA256: {Hash}", hash);
                    return hash;
                }
            }
        }
    }
}