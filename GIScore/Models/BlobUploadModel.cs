using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GIScore.Models
{
    public class BlobUploadModel
    {
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public long FileSizeBt { get; set; }
        public long FileSizeKB { get { return (long)Math.Ceiling((double)FileSizeBt / 1024); } }
    }
}