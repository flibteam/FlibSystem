namespace FlibSystem.Models
{
    public class BackupEntry
    {
        public string Folder { get; set; }
        public string Created { get; set; }
        public string FileCount { get; set; }
        public string DisplayName
        {
            get
            {
                int idx = Folder.LastIndexOf('\\');
                return idx >= 0 ? Folder.Substring(idx + 1) : Folder;
            }
        }
    }
}

