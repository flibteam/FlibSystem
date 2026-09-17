using System;

namespace FlibSystem.Models
{
    public class ReleaseInfo
    {
        public string TagName { get; set; }
        public string Name { get; set; }
        public string Body { get; set; }
        public DateTime PublishedAt { get; set; }
        public string AssetName { get; set; }
        public string AssetUrl { get; set; }
        public long AssetSize { get; set; }
        public bool HasAsset { get; set; }
        public string HashFileName { get; set; }
        public string HashUrl { get; set; }
        public bool HasHashFile { get; set; }

        public string VersionText => TagName ?? "0.0.0";
    }
}