using System;

namespace EasyVan.Models
{
    public class Van
    {
        public int Id { get; set; }
        public string Plate { get; set; } = string.Empty;
        public string Driver { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string Route { get; set; } = string.Empty;
        public string Schedule { get; set; } = string.Empty;
        public string Status { get; set; } = "Operacional";
        public string Description { get; set; } = string.Empty;
    }
}
