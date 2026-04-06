namespace farm2homeWebApi.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Image { get; set; }
    public string Type { get; set; }
    public decimal Price { get; set; }
    public string? Unit { get; set; }       // "500g", "1 Trái (1.2-1.5kg)"
    public int Stock { get; set; }          // Số lượng tồn kho
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } // Tên danh mục (FE dùng string category, không phải id)
}

public class ProductsPageResponse
{
    public List<ProductDto> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
