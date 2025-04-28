using NLog;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using NorthwindConsole.Model;
using System.ComponentModel.DataAnnotations;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
string path = Directory.GetCurrentDirectory() + "//nlog.config";

// create instance of Logger
var logger = LogManager.Setup().LoadConfigurationFromFile(path).GetCurrentClassLogger();
logger.Info("Program started");

var configuration = new ConfigurationBuilder().AddJsonFile($"appsettings.json");
var config = configuration.Build();

do
{
    Console.WriteLine("1) Display Categories");
    Console.WriteLine("2) Add Category");
    Console.WriteLine("3) Display Category and related Products");
    Console.WriteLine("4) Display all Categories and their related Products");
    Console.WriteLine("5) Display Products");
    Console.WriteLine("6) Display Product Info");
    Console.WriteLine("7) Add Product");
    Console.WriteLine("8) Remove Product");
    Console.WriteLine("0) Quit");
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
            DisplayProducts();
            break;
        case "6":
            DisplayProductInfo();
            break;
        case "7":
            AddProduct();
            break;
        case "8":
            RemoveProduct();
            break;
        default:
            Environment.Exit(0);
            break;
    }

    void DisplayCategories()
    {
        // display categories
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

    Supplier GetSupplier()
    {
        // get supplier
        var db = new DataContext();
        var query = db.Suppliers.OrderBy(p => p.SupplierId);

        Console.WriteLine("Select supplier:");
        Console.ForegroundColor = ConsoleColor.DarkRed;
        foreach (var item in query)
        {
            Console.WriteLine($"{item.SupplierId}) {item.CompanyName}");
        }
        Console.ForegroundColor = ConsoleColor.White;
        int id = int.Parse(Console.ReadLine()!);
        Console.Clear();
        logger.Info($"SupplierId {id} selected");
        Supplier supplier = db.Suppliers.Include("Products").FirstOrDefault(c => c.SupplierId == id)!;
        if (supplier == null)
        {
            Console.WriteLine("Supplier not found");
            return null!;
        }
        return supplier;
    }

    Category GetCategory()
    {
        // get category
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
        // display all categories and related products
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

    void DisplayProducts()
    {
        // display products
        Console.WriteLine("Display which Products:");
        Console.WriteLine("1) All Products");
        Console.WriteLine("2) Active Products only");
        Console.WriteLine("3) Discontinued Products only");
        string choice = Console.ReadLine()!;
        Console.Clear();
        switch (choice)
        {
            case "1":
                DisplayAllProducts();
                break;
            case "2":
                DisplayAllActiveProducts();
                break;
            case "3":
                DisplayAllDiscontinuedProducts();
                break;
            default:
                Console.WriteLine("Invalid choice");
                return;
        }
    }

    void DisplayAllProducts()
    {
        // display all products
        var db = new DataContext();
        logger.Info("Display All Products selected");
        Console.WriteLine("All Products - Discontinued in Gray");
        var query = db.Products.OrderBy(p => p.ProductName);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"{query.Count()} records returned");
        foreach (var item in query)
        {
            Console.ForegroundColor = item.Discontinued? ConsoleColor.DarkGray : ConsoleColor.Cyan;
            Console.WriteLine($"{item.ProductName}");
        }
        Console.ForegroundColor = ConsoleColor.White;
    }

    void DisplayAllActiveProducts()
        {
            // display active products
            var db = new DataContext();
            logger.Info("Display Active Products selected");
            Console.WriteLine("Active Products");
            var query = db.Products.Where(p => p.Discontinued == false).OrderBy(p => p.ProductName);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{query.Count()} records returned");
            Console.ForegroundColor = ConsoleColor.Cyan;
            foreach (var item in query)
            {
                Console.WriteLine($"{item.ProductName}");
            }
            Console.ForegroundColor = ConsoleColor.White;
        }

    void DisplayAllDiscontinuedProducts()
    {
        // display discontinued products
        logger.Info("Display Discontinued Products selected");
        var db = new DataContext();
        var query = db.Products.Where(p => p.Discontinued == true).OrderBy(p => p.ProductName);
        Console.WriteLine("Discontinued Products");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"{query.Count()} records returned");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        foreach (var item in query)
        {
            Console.WriteLine($"{item.ProductName}");
        }
    Console.ForegroundColor = ConsoleColor.White;
    }

    int GetIntFromReply(string reply)
    {
        // get int from reply
        int id = 0;
        try
        {
            id = int.Parse(reply);
            return id;
        }
        catch (FormatException ex)
        {
            Console.WriteLine("Invalid input. Please enter a number.");
            logger.Error(ex, "Invalid input");
            return 0;
        }
        catch (OverflowException ex)
        {
            Console.WriteLine("Input is too large. Please enter a smaller number.");
            logger.Error(ex, "Input is too large");
            return 0;
        }
        catch (ArgumentNullException ex)
        {
            Console.WriteLine("Input cannot be null. Please enter a number.");
            logger.Error(ex, "Input is null");
            return 0;
        }
    }

    void DisplayProductInfo()
    {
        // display product info
        var db = new DataContext();
        var query = db.Products.OrderBy(p => p.ProductId);
        Console.ForegroundColor = ConsoleColor.DarkRed;
        foreach (var item in query)
        {
            Console.WriteLine($"{item.ProductId}) {item.ProductName}");
        }
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Select product:");
        // handle exception if user enters invalid input
        int id = GetIntFromReply(Console.ReadLine()!);
        Console.Clear();
        logger.Info($"ProductId {id} selected");
        Product product = db.Products.FirstOrDefault(c => c.ProductId == id)!;
        if (product == null)
        {
            Console.WriteLine("Product not found");
            return;
        }
        Console.WriteLine($"Product Id: {product.ProductId}");
        Console.WriteLine($"Product Name: {product.ProductName}");
        Console.WriteLine($"Supplier Id: {product.SupplierId}");
        Console.WriteLine($"Category Id: {product.CategoryId}");
        Console.WriteLine($"Quantity Per Unit: {product.QuantityPerUnit}");
        Console.WriteLine($"Unit Price: {product.UnitPrice}");
        Console.WriteLine($"Units In Stock: {product.UnitsInStock}");
        Console.WriteLine($"Units On Order: {product.UnitsOnOrder}");
        Console.WriteLine($"Reorder Level: {product.ReorderLevel}");
        Console.WriteLine($"Discontinued: {product.Discontinued}");
    }

    void AddProduct()
    {
        // Add product
        Product product = new();
        Console.WriteLine("Enter Product Name:");
        product.ProductName = Console.ReadLine()!;
        if (string.IsNullOrEmpty(product.ProductName))
        {
            Console.WriteLine("Product name cannot be empty");
            return;
        }
        Console.WriteLine("Enter the Quantity per Unit:");
        product.QuantityPerUnit = Console.ReadLine()!;

        Console.WriteLine("Enter the Unit Price:");
        string unitPrice = Console.ReadLine()!;
        try
        {
            product.UnitPrice = decimal.Parse(unitPrice);
        }
        catch (FormatException ex)
        {
            Console.WriteLine("Invalid input. Please enter a number.");
            logger.Error(ex, "Invalid input");
            return;
        }
        catch (OverflowException ex)
        {
            Console.WriteLine("Input is too large. Please enter a smaller number.");
            logger.Error(ex, "Input is too large");
            return;
        }
        catch (ArgumentNullException ex)
        {
            Console.WriteLine("Input cannot be null. Please enter a number.");
            logger.Error(ex, "Input is null");
            return;
        }

        Console.WriteLine("Enter the Units In Stock:");
        string unitsInStock = Console.ReadLine()!;
        try
        {
            product.UnitsInStock = short.Parse(unitsInStock);
        }
        catch (FormatException ex)
        {
            Console.WriteLine("Invalid input. Please enter a number.");
            logger.Error(ex, "Invalid input");
            return;
        }
        catch (OverflowException ex)
        {
            Console.WriteLine("Input is too large. Please enter a smaller number.");
            logger.Error(ex, "Input is too large");
            return;
        }
        catch (ArgumentNullException ex)
        {
            Console.WriteLine("Input cannot be null. Please enter a number.");
            logger.Error(ex, "Input is null");
            return;
        }

        Console.WriteLine("Enter the Units On Order:");
        string unitsOnOrder = Console.ReadLine()!;
        try
        {
            product.UnitsOnOrder = short.Parse(unitsOnOrder);
        }
        catch (FormatException ex)
        {
            Console.WriteLine("Invalid input. Please enter a number.");
            logger.Error(ex, "Invalid input");
            return;
        }
        catch (OverflowException ex)
        {
            Console.WriteLine("Input is too large. Please enter a smaller number.");
            logger.Error(ex, "Input is too large");
            return;
        }
        catch (ArgumentNullException ex)
        {
            Console.WriteLine("Input cannot be null. Please enter a number.");
            logger.Error(ex, "Input is null");
            return;
        }
        Console.WriteLine("Enter the Reorder Level:");
        string reorderLevel = Console.ReadLine()!;
        try
        {
            product.ReorderLevel = short.Parse(reorderLevel);
        }
        catch (FormatException ex)
        {
            Console.WriteLine("Invalid input. Please enter a number.");
            logger.Error(ex, "Invalid input");
            return;
        }
        catch (OverflowException ex)
        {
            Console.WriteLine("Input is too large. Please enter a smaller number.");
            logger.Error(ex, "Input is too large");
            return;
        }
        catch (ArgumentNullException ex)
        {
            Console.WriteLine("Input cannot be null. Please enter a number.");
            logger.Error(ex, "Input is null");
            return;
        }

        Category category = GetCategory();

        Supplier supplier = GetSupplier();

        Console.WriteLine("Is the product discontinued? (y/n)");
        string discontinued = Console.ReadLine()!;
        if (discontinued.ToLower() == "y")
        {
            product.Discontinued = true;
        }
        else if (discontinued.ToLower() == "n")
        {
            product.Discontinued = false;
        }
        else
        {
            Console.WriteLine("Invalid input. Please enter y or n.");
            return;
        }

        // validate product
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
                product.SupplierId = supplier.SupplierId;
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
