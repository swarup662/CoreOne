using System;
using System.Collections.Generic;

namespace CoreOne.COMMON.Models
{
    public class UploadModuleConfig
    {
        public string Module { get; set; }
        public string UploadPath { get; set; }
        public List<string> AllowedExtensions { get; set; }
        public int MaxSizeInMB { get; set; }

        // New property: optional compression level (0-100 for images/PDFs)
        public int CompressionLevel { get; set; } = 50; // default 50 if not set
    }

    public class FileUploadSettings
    {
        public List<UploadModuleConfig> UploadModules { get; set; }
    }
}
