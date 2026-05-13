namespace LibraryManagement.Api.Models.DTOs.Books;

public class CreateBookRequest
{
    public string ISBN { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Publisher { get; set; }
    public string? Category { get; set; }
    public decimal? Price { get; set; }
    public int TotalCopies { get; set; }
    public string? Location { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Description { get; set; }
}

public class UpdateBookRequest
{
    public string ISBN { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Publisher { get; set; }
    public string? Category { get; set; }
    public decimal? Price { get; set; }
    public int TotalCopies { get; set; }
    public string? Location { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Description { get; set; }
}

public class BookDto
{
    public int Id { get; set; }
    public string ISBN { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Publisher { get; set; }
    public string? Category { get; set; }
    public decimal? Price { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public string? Location { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
