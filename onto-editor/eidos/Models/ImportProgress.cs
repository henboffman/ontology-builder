namespace Eidos.Models
{
    public class ImportProgress
    {
        public string Stage { get; set; } = "";
        public int Current { get; set; }
        public int Total { get; set; }
        public int Percentage => Total > 0 ? (int)((Current / (double)Total) * 100) : 0;
        public string Message { get; set; } = "";
    }
}
