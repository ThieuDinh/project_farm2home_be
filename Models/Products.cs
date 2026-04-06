namespace farm2homeWebApi.Models;

public class Products
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Image { get; set; }
    public string Type { get; set; } 
    public decimal Price { get; set; }
    public string? Unit { get; set; }
    public int Stock { get; set; } = 0;
    public int CategoryId { get; set; }
    public Categories Category { get; set; }
}
