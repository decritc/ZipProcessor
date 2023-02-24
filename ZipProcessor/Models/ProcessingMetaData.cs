using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZipProcessor.Models
{
    public class ProcessingMetaData
    {
        public List<ProcessedZipFile> ProcessedZipFiles { get; set; }

        public ProcessingMetaData()
        {
            ProcessedZipFiles = new List<ProcessedZipFile>();
        }
    }
}
