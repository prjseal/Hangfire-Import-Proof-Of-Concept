using System.Text.Json.Serialization;

namespace MyProject.Models;

public class CentreModel
{
    public string name { get; set; }
    public decimal latitude { get; set; }
    public decimal longitude { get; set; }
    public DateTime lastModifiedDate { get; set; }
    public Guid systemid { get; set; }
}
