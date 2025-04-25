using NLog;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using NorthwindConsole.Model;
using System.ComponentModel.DataAnnotations;
using Microsoft.Identity.Client;
string path = Directory.GetCurrentDirectory() + "//nlog.config";

// create instance of Logger
var logger = LogManager.Setup().LoadConfigurationFromFile(path).GetCurrentClassLogger();

logger.Info("Program started");

do
{
    Console.WriteLine("1) Display categories");
    Console.WriteLine("2) Add category");
    Console.WriteLine("3) Display Category and related products");
    Console.WriteLine("4) Display all Categories and their related products");
    Console.WriteLine("5) Add product");
    Console.WriteLine("6) Remove product");
    Console.WriteLine("q) Quit");
    string? choice = Console.ReadLine();
    Console.Clear();
    logger.Info("Option {choice} selected", choice);

    switch (choice)
    {
        case "1":
            DisplayCategories();
            break;
        case "2":
            AddCategory();
            break;
        case "3":
            DisplayCategoryProducts();
            break;
        case "4":
            DisplayAllCategoriesAndProducts();
            break;
        case "5":
            AddProduct();
            break;
        case "6":
            RemoveProduct();
            break;
        case "q":
            Environment.Exit(0);
            break;
        default:
            break;
    }

    void DisplayCategories()
    {
        // display categories
        var configuration = new ConfigurationBuilder().AddJsonFile($"appsettings.json");

        var config = configuration.Build();

        var db = new DataContext();
        var query = db.Categories.OrderBy(p => p.CategoryName);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"{query.Count()} records returned");
        Console.ForegroundColor = ConsoleColor.Magenta;
        foreach (var item in query)
        {
            Console.WriteLine($"{item.CategoryName} - {item.Description}");
        }
        Console.ForegroundColor = ConsoleColor.White;
    }

    void AddCategory()
    {
        // Add category
        Category category = new();

        Console.WriteLine("Enter Category Name:");
        string categoryName = Console.ReadLine()!;
        if (string.IsNullOrEmpty(categoryName))
        {
            Console.WriteLine("Category name cannot be empty");
            return;
        } else
        {
            category.CategoryName = categoryName;
        }

        Console.WriteLine("Enter the Category Description:");
        string description = Console.ReadLine()!;
        if (string.IsNullOrEmpty(description))
        {
            Console.WriteLine("Category description cannot be empty");
            return;
        } else
        {
            category.Description = description;
        }

        ValidationContext context = new(category, null, null);
        List<ValidationResult> results = [];

        var isValid = Validator.TryValidateObject(category, context, results, true);
        if (isValid)
        {
            var db = new DataContext();
            // check for unique name
            if (db.Categories.Any(c => c.CategoryName == category.CategoryName))
            {
                // generate validation error
                isValid = false;
                results.Add(new ValidationResult("Name exists", ["CategoryName"]));
            }
            else
            {
                logger.Info("Validation passed");
                db.Categories.Add(category);
                db.SaveChanges();
                logger.Info("Category added to database");
                Console.WriteLine($"{category.CategoryName} - {category.Description} added to database");
            }
        }
        if (!isValid)
        {
            foreach (var result in results)
            {
                logger.Error($"{result.MemberNames.First()} : {result.ErrorMessage}");
            }
        }
    }

    void DisplayCategoryProducts()
    {
        // display category and related products
        Category category = GetCategory();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"{category.CategoryName} - {category.Description}");
        Console.ForegroundColor = ConsoleColor.Magenta;
        foreach (Product p in category.Products)
        {
            Console.WriteLine($"\t{p.ProductName}");
        }
        Console.ForegroundColor = ConsoleColor.White;
    }

    Category GetCategory()
    {
        var configuration = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json");

        var config = configuration.Build();

        var db = new DataContext();
        var query = db.Categories.OrderBy(p => p.CategoryId);

        Console.WriteLine("Select category:");
        Console.ForegroundColor = ConsoleColor.DarkRed;
        foreach (var item in query)
        {
            Console.WriteLine($"{item.CategoryId}) {item.CategoryName}");
        }
        Console.ForegroundColor = ConsoleColor.White;
        int id = int.Parse(Console.ReadLine()!);
        Console.Clear();
        logger.Info($"CategoryId {id} selected");
        Category category = db.Categories.Include("Products").FirstOrDefault(c => c.CategoryId == id)!;
        if (category == null)
        {
            Console.WriteLine("Category not found");
            return null!;
        }
        return category;
    }

    void DisplayAllCategoriesAndProducts()
    {
        var db = new DataContext();
        var query = db.Categories.Include("Products").OrderBy(p => p.CategoryId);
        foreach (var item in query)
        {
            Console.WriteLine($"\n{item.CategoryName}");
            foreach (Product p in item.Products)
            {
                Console.WriteLine($"\t{p.ProductName}");
            }
        }
    }

    void AddProduct()
    {
        // Add product
        Product product = new();
        Category category = GetCategory();
        if (category == null)
        {
            Console.WriteLine("Please select a valid category");
            return;
        }
        
        Console.WriteLine("Enter Product Name:");
        product.ProductName = Console.ReadLine()!;
        if (string.IsNullOrEmpty(product.ProductName))
        {
            Console.WriteLine("Product name cannot be empty");
            return;
        }

        ValidationContext context = new(product, null, null);
        List<ValidationResult> results = [];

        var isValid = Validator.TryValidateObject(product, context, results, true);
        if (isValid)
        {
            var db = new DataContext();
            // check for unique name
            if (db.Products.Any(c => c.ProductName == product.ProductName))
            {
                // generate validation error
                isValid = false;
                results.Add(new ValidationResult("Name exists", ["ProductName"]));
            }
            else
            {
                logger.Info("Validation passed");
                product.CategoryId = category.CategoryId;
                db.Products.Add(product);

                db.SaveChanges();
                logger.Info("Product added to database");
                Console.WriteLine($"{product.ProductName} - {category.CategoryName} added to database");
            }
        }
    }

    void RemoveProduct()
    {
        // remove product
        var db = new DataContext();
        var query = db.Products.OrderBy(p => p.ProductId);
        Console.WriteLine("Select product to remove:");
        Console.ForegroundColor = ConsoleColor.DarkRed;
        foreach (var item in query)
        {
            Console.WriteLine($"{item.ProductId}) {item.ProductName}");
        }
        Console.ForegroundColor = ConsoleColor.White;
        int id = int.Parse(Console.ReadLine()!);
        Product product = db.Products.FirstOrDefault(c => c.ProductId == id)!;
        if (product == null)
        {
            Console.WriteLine("Product not found");
            return;
        }
        db.Products.Remove(product);
        db.SaveChanges();
        logger.Info("Product removed from database");
        Console.WriteLine($"{product.ProductName} removed from database");
    }

    Console.WriteLine();

} while (true);
