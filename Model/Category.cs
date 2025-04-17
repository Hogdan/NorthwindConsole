using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NorthwindConsole.Model;

public partial class Category
{
  public int CategoryId { get; set; }
  [Required(ErrorMessage = "You need a proper name buddy!")]
  public string CategoryName { get; set; } = null!;

  public string? Description { get; set; }

  public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}