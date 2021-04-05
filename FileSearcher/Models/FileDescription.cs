using System;

namespace FileSearcher.Models
{
    class FileDescription
    {
        public string Name
        {
            get; set;
        }
        public string Path
        {
            get; set;
        }
        public long Size
        {
            get; set;
        }
        public DateTime CreatedTime
        {
            get; set;
        }
    }
}
