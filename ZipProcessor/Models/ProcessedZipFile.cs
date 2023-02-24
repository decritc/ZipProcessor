namespace ZipProcessor.Models
{
    public class ProcessedZipFile
    {
        public string Name { get; set; }
        public List<PdfFile> PdfFiles { get; set; }
        public DateTimeOffset DateProcessed { get; set; }

        public ProcessedZipFile(string name)
        {
            Name = name;
            PdfFiles = new List<PdfFile>();
        }
    }
}