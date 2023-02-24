namespace ZipProcessor.Models
{
    public class PdfFile
    {
        public string Name { get; set; }

        public PdfFile(string name) 
        {
            Name = name;
        }
    }
}