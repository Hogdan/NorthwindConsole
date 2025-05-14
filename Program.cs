using NLog;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using NorthwindConsole.Model;
using System.ComponentModel.DataAnnotations;
using Microsoft.IdentityModel.Tokens;
string path = Directory.GetCurrentDirectory() + "//nlog.config";

// create instance of Logger
var logger = LogManager.Setup().LoadConfigurationFromFile(path).GetCurrentClassLogger();
logger.Info("Program started");

var configuration = new ConfigurationBuilder().AddJsonFile($"appsettings.json");
var config = configuration.Build();

do
{
    // display main menu
    Console.WriteLine("1) Categories Menu");
    Console.WriteLine("2) Products Menu");
    Console.WriteLine("0) Quit");
    string? choice = Console.ReadLine();
    Console.Clear();
    logger.Info("Option {choice} selected", choice);

    switch (choice)
    {
        case "1":
            DisplayCategoriesMenu();
            break;
        case "2":
            DisplayProductMenu();
            break;
        default:
            logger.Info("Exiting program");
            Environment.Exit(0);
            break;
    }

    void DisplayCategoriesMenu()
    {
        // display categories menu
        Console.WriteLine("1) Display All Categories");
        Console.WriteLine("2) Display All Categories & Active Products");
        Console.WriteLine("3) Display Single Category & Active Products");
        Console.WriteLine("4) Add Category");
        Console.WriteLine("5) Edit Category");
        Console.WriteLine("6) Remove Category");
        Console.WriteLine("0) Return to Main Menu");
        string? choice = Console.ReadLine()!;
        Console.Clear();
        logger.Info("Option {choice} selected", choice);
        switch (choice)
        {
            case "1":
                DisplayCategories();
                break;
            case "2":
                DisplayAllCategoriesAndProducts();
                break;
            case "3":
                Category? category = GetCategory();
                if (category == null) break;
                DisplayCategoryProducts(category);
                break;
            case "4":
                AddCategory();
                break;
            case "5":
                EditCategory();
                break;
            case "6":
                RemoveCategory();
                break;
            default:
                return;
        }
    }

    void DisplayProductMenu()
    {
        // display product menu
        Console.WriteLine("1) Display Products");
        Console.WriteLine("2) Display Product Details");
        Console.WriteLine("3) Add Product");
        Console.WriteLine("4) Edit Product");
        Console.WriteLine("5) Remove Product");
        Console.WriteLine("0) Return to Main Menu");
        string? choice = Console.ReadLine()!;
        Console.Clear();
        logger.Info("Option {choice} selected", choice);
        switch (choice)
        {
            case "1":
                DisplayProducts();
                break;
            case "2":
                DisplayProductDetails();
                break;
            case "3":
                AddProduct();
                break;
            case "4":
                EditProduct();
                break;
            case "5":
                RemoveProduct();
                break;
            default:
                return;
        }
    }

    void DisplayCategories()
    {
        // display categories
        logger.Info("Display Categories selected");
        var db = new DataContext();
        var query = db.Categories.OrderBy(p => p.CategoryName);

        Console.WriteLine($"{query.Count()} Categories found");

        Console.ForegroundColor = ConsoleColor.Green;
        foreach (var item in query)
        {
            Console.WriteLine($"{item.CategoryName} - {item.Description}");
        }
        Console.ForegroundColor = ConsoleColor.White;
    }

    void DisplayCategoryProducts(Category category)
    {
        // display category and related active products
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n{category.CategoryName} - {category.Description}");

        Console.ForegroundColor = ConsoleColor.Magenta;
        category.Products = [.. category.Products.Where(p => p.Discontinued == false).OrderBy(p => p.ProductName)];
        foreach (Product p in category.Products)
        {
            Console.WriteLine($"\t{p.ProductName}");
        }
        Console.ForegroundColor = ConsoleColor.White;
    }

    void DisplayAllCategoriesAndProducts()
    {
        // display all categories and related products
        var db = new DataContext();
        var query = db.Categories.Include("Products").OrderBy(p => p.CategoryId);
        foreach (var category in query)
        {
            DisplayCategoryProducts(category);
        }
    }

    void AddCategory()
    {
        // Add category
        Category category = new();

        string? categoryName = GetStringInput("Enter Category Name:", "Category name cannot be empty");
        if (categoryName.IsNullOrEmpty()) return;
        else category.CategoryName = categoryName!;

        string? description = GetStringInput("Enter Category Description:", "Category description cannot be empty");
        if (description.IsNullOrEmpty()) return;
        else category.Description = description!;

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
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{category.CategoryName} - {category.Description} added to database");
                Console.ForegroundColor = ConsoleColor.White;
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

    void EditCategory()
    {
        // edit category
        Category? category = GetCategory();
        if (category == null) return;
        Console.WriteLine($"Enter New Category Name or Leave Blank to Keep: {category.CategoryName}");
        string? categoryName = Console.ReadLine();
        if (!categoryName.IsNullOrEmpty()) category.CategoryName = categoryName!;

        Console.WriteLine($"Enter New Category Description or Leave Blank to Keep: {category.Description}");
        string? description = Console.ReadLine();
        if (!description.IsNullOrEmpty()) category.Description = description!;

        var db = new DataContext();
        db.Categories.Update(category);
        db.SaveChanges();
        logger.Info("Category updated in database");
        Console.WriteLine($"{category.CategoryName} - {category.Description} updated in database");
    }

    void RemoveCategory()
    {
        // remove category
        Category? category = GetCategory();
        if (category == null) return;
        var db = new DataContext();
        db.Categories.Remove(category);
        db.SaveChanges();
        logger.Info("Category removed from database");
        Console.WriteLine($"{category.CategoryName} removed from database");
    }

    void DisplayProducts()
    {
        // display products
        Console.WriteLine("Display which Products:");
        Console.WriteLine("1) All Products");
        Console.WriteLine("2) Active Products only");
        Console.WriteLine("3) Discontinued Products only");
        string? choice = Console.ReadLine()!;
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
        logger.Info("Display All Products selected");
        Console.WriteLine("All Products - Discontinued in Gray");
        var db = new DataContext();
        var query = db.Products.OrderBy(p => p.ProductName);
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"{query.Count()} Products returned");
        foreach (var item in query)
        {
            Console.ForegroundColor = item.Discontinued? ConsoleColor.DarkGray : ConsoleColor.Magenta;
            Console.WriteLine($"{item.ProductName}");
        }
        Console.ForegroundColor = ConsoleColor.White;
    }

    void DisplayAllActiveProducts()
        {
            // display active products
            logger.Info("Display Active Products selected");
            Console.WriteLine("Active Products");
            var db = new DataContext();
            var query = db.Products.Where(p => p.Discontinued == false).OrderBy(p => p.ProductName);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{query.Count()} records returned");
            Console.ForegroundColor = ConsoleColor.Magenta;
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
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"{query.Count()} records returned");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        foreach (var item in query)
        {
            Console.WriteLine($"{item.ProductName}");
        }
    Console.ForegroundColor = ConsoleColor.White;
    }

    void DisplayProductDetails()
    {
        // display product info
        Product? product = GetProduct();
        if (product == null) return;
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"Product Id: {product.ProductId}");
        Console.WriteLine($"Product Name: {product.ProductName}");
        Console.WriteLine($"Supplier Id: {product.SupplierId}");
        Console.WriteLine($"Category Id: {product.CategoryId}");
        Console.WriteLine($"Quantity Per Unit: {product.QuantityPerUnit}");
        Console.WriteLine($"Unit Price: {product.UnitPrice:F2}");
        Console.WriteLine($"Units In Stock: {product.UnitsInStock}");
        Console.WriteLine($"Units On Order: {product.UnitsOnOrder}");
        Console.WriteLine($"Reorder Level: {product.ReorderLevel}");
        Console.WriteLine($"Discontinued: {product.Discontinued}");
        Console.ForegroundColor = ConsoleColor.White;
    }

    void AddProduct()
    {
        // Add product
        Product product = new();
        string? productName = GetStringInput("Enter Product Name:", "Product name cannot be empty");
        if (string.IsNullOrEmpty(productName)) return;

        string? quantityPerUnit = GetStringInput("Enter the Quantity per Unit:", "Quantity per unit cannot be empty");
        if (string.IsNullOrEmpty(quantityPerUnit)) return;

        decimal? unitPrice = GetDecimalInput("Enter the Unit Price:");
        if (unitPrice == null) return;

        short? unitsInStock = GetShortInput("Enter the Units In Stock:");
        if (unitsInStock == null) return;

        short? unitsOnOrder = GetShortInput("Enter the Units On Order:");
        if (unitsOnOrder == null) return;

        short? reorderLevel = GetShortInput("Enter the Reorder Level:");
        if (reorderLevel == null) return;

        Category? category = GetCategory();
        if (category == null) return;

        Supplier? supplier = GetSupplier();
        if (supplier == null) return;

        string discontinued = GetStringInput("Is the product discontinued? (y/n):", "Invalid input. Please enter y or n")!;
        if (discontinued.Equals("y", StringComparison.CurrentCultureIgnoreCase)) product.Discontinued = true;
        else if (discontinued.Equals("n", StringComparison.CurrentCultureIgnoreCase)) product.Discontinued = false;
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
                product.ProductName = productName!;
                product.QuantityPerUnit = quantityPerUnit!;
                product.UnitPrice = unitPrice!;
                product.UnitsInStock = unitsInStock!;
                product.UnitsOnOrder = unitsOnOrder!;
                product.CategoryId = category.CategoryId;
                product.SupplierId = supplier.SupplierId;
                db.Products.Add(product);

                db.SaveChanges();
                logger.Info("Product added to database");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"{product.ProductName} - {category.CategoryName} added to database");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
    }

    void EditProduct()
    {
        // edit product
        Product? product = GetProduct();
        if (product == null) return;
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"Editing: {product.ProductName}");
        Console.ForegroundColor = ConsoleColor.White;

        Console.WriteLine($"Enter New Product Name or Leave Blank to keep: {product.ProductName}");  
        string? productName = Console.ReadLine();
        if (!string.IsNullOrEmpty(productName)) product.ProductName = productName!;

        Console.WriteLine($"Enter New Quantity Per Unit or Leave Blank to keep: {product.QuantityPerUnit}");
        string? quantityPerUnit = Console.ReadLine();
        if (!string.IsNullOrEmpty(quantityPerUnit)) product.QuantityPerUnit = quantityPerUnit!;

        Console.WriteLine($"Current Supplier Id: {product.SupplierId}");
        Supplier? supplier = GetSupplier();
        if (supplier == null) return;
        else product.SupplierId = supplier.SupplierId;

        Console.WriteLine($"Current Category Id: {product.CategoryId}");
        Category? category = GetCategory();
        if (category == null) return;
        else product.CategoryId = category.CategoryId;

        Console.WriteLine($"Current Unit Price: {product.UnitPrice}");
        decimal? unitPrice = GetDecimalInput("Enter new Unit Price:");
        if (unitPrice == null) return;
        else product.UnitPrice = unitPrice.Value;

        Console.WriteLine($"Current Units In Stock: {product.UnitsInStock}");
        short? unitsInStock = GetShortInput("Enter new Units In Stock:");
        if (unitsInStock == null) return;
        else product.UnitsInStock = unitsInStock.Value;

        Console.WriteLine($"Current Units On Order: {product.UnitsOnOrder}");
        short? unitsOnOrder = GetShortInput("Enter new Units On Order:");
        if (unitsOnOrder == null) return;
        else product.UnitsOnOrder = unitsOnOrder.Value;

        Console.WriteLine($"Current Reorder Level: {product.ReorderLevel}");
        short? reorderLevel = GetShortInput("Enter new Reorder Level:");
        if (reorderLevel == null) return;
        else product.ReorderLevel = reorderLevel.Value;

        Console.WriteLine($"Currently Discontinued: {product.Discontinued}");
        string? discontinued = GetStringInput("Is the product discontinued? (y/n):", "Invalid input. Please enter y or n")!;
        if (discontinued.Equals("y", StringComparison.CurrentCultureIgnoreCase)) product.Discontinued = true;
        else if (discontinued.Equals("n", StringComparison.CurrentCultureIgnoreCase)) product.Discontinued = false;
        else
        {
            Console.WriteLine("Invalid input. Please enter y or n.");
            return;
        }


        var db = new DataContext();
        db.Products.Update(product);
        db.SaveChanges();
        logger.Info($"{product.ProductName} updated in database");
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"{product.ProductName} updated in database");
        Console.ForegroundColor = ConsoleColor.White;
    }

    void RemoveProduct()
    {
        // remove product
        Product? product = GetProduct();
        if (product == null) return;
        var db = new DataContext();
        db.Products.Remove(product);
        db.SaveChanges();
        logger.Info("Product removed from database");
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"{product.ProductName} removed from database");
        Console.ForegroundColor = ConsoleColor.White;
    }

    string? GetStringInput(string prompt, string errorMsg)
    {
        // get string input
        Console.WriteLine(prompt);
        string? input = Console.ReadLine()!;
        if (string.IsNullOrEmpty(input))
        {
            Console.WriteLine(errorMsg);
            return null;
        }
        return input;
    }

    int? GetIntInput()
    {
        // get int from reply
        int id = 0;
        string? reply = Console.ReadLine()!;
        if (string.IsNullOrEmpty(reply))
        {
            Console.WriteLine("Please enter a number.");
            logger.Error("Input is null or empty");
            return null;
        }
        try
        {
            id = int.Parse(reply);
            logger.Info($"Input {id} received");
            return id;
        }
        catch (FormatException ex)
        {
            Console.WriteLine("Invalid input. Please enter a number.");
            logger.Error(ex, "Invalid input");
            return null;
        }
        catch (OverflowException ex)
        {
            Console.WriteLine("Input is too large. Please enter a smaller number.");
            logger.Error(ex, "Input is too large");
            return null;
        }
        catch (ArgumentNullException ex)
        {
            Console.WriteLine("Input cannot be null. Please enter a number.");
            logger.Error(ex, "Input is null");
            return null;
        }
    }

    decimal? GetDecimalInput(string prompt)
    {
        Console.WriteLine(prompt);
        string? input = Console.ReadLine();
        if (string.IsNullOrEmpty(input))
        {
            Console.WriteLine("Please enter a number.");
            logger.Error("Input is null or empty");
            return null;
        }
        // check for valid decimal input
        try
        {
            return decimal.Abs(decimal.Parse(input!));
        }
        catch (FormatException ex)
        {
            Console.WriteLine("Invalid input. Please enter a number.");
            logger.Error(ex, "Invalid input");
        }
        catch (OverflowException ex)
        {
            Console.WriteLine("Input is too large. Please enter a smaller number.");
            logger.Error(ex, "Input is too large");
        }
        return null;
    }

    short? GetShortInput(string prompt)
    {
        Console.WriteLine(prompt);
        string? input = Console.ReadLine();
        if (string.IsNullOrEmpty(input))
        {
            Console.WriteLine("Please enter a number.");
            logger.Error("Input is null or empty");
            return null;
        }
        // check for valid short input
        try
        {
            return short.Abs(short.Parse(input));
        }
        catch (FormatException ex)
        {
            Console.WriteLine("Invalid input. Please enter a number.");
            logger.Error(ex, "Invalid input");
        }
        catch (OverflowException ex)
        {
            Console.WriteLine("Input is too large. Please enter a smaller number.");
            logger.Error(ex, "Input is too large");
        }
        return null;
    }

    Product? GetProduct()
    {
        // get product
        var db = new DataContext();
        var query = db.Products.OrderBy(p => p.ProductId);
        Console.WriteLine("Select product:");
        Console.ForegroundColor = ConsoleColor.Magenta;
        foreach (var item in query)
        {
            Console.WriteLine($"{item.ProductId}) {item.ProductName}");
        }
        Console.ForegroundColor = ConsoleColor.White;
        int? id = GetIntInput();
        if (id == null) return null;

        logger.Info($"ProductId {id} selected");
        Product product = db.Products.FirstOrDefault(c => c.ProductId == id)!;
        if (product == null)
        {
            logger.Error("Product not found");
            Console.WriteLine("Product not found");
            return null;
        }
        logger.Info($"Product {product.ProductName} selected");
        return product;
    }

    Supplier? GetSupplier()
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
        int? id = GetIntInput();
        if (id == null)
        {
            logger.Error("Invalid input");
            Console.WriteLine("Please enter a valid number.");
            return null;
        }
        logger.Info($"SupplierId {id} selected");
        Supplier supplier = db.Suppliers.Include("Products").FirstOrDefault(c => c.SupplierId == id)!;
        if (supplier == null)
        {
            logger.Error("Supplier not found");
            Console.WriteLine("Supplier not found");
            return null!;
        }
        return supplier;
    }

    Category? GetCategory()
    {
        // get category
        var db = new DataContext();
        var query = db.Categories.OrderBy(p => p.CategoryId);

        Console.WriteLine("Select category:");
        Console.ForegroundColor = ConsoleColor.Green;
        foreach (var item in query)
        {
            Console.WriteLine($"{item.CategoryId}) {item.CategoryName}");
        }
        Console.ForegroundColor = ConsoleColor.White;
        int? id = GetIntInput();
        if (id == null) return null;

        logger.Info($"CategoryId {id} selected");
        Category category = db.Categories.Include("Products").FirstOrDefault(c => c.CategoryId == id)!;
        if (category == null)
        {
            logger.Error("Category not found");
            Console.WriteLine("Category not found");
            return null;
        }
        logger.Info($"Category {category.CategoryName} selected");
        Console.Clear();
        return category;
    }

    Console.WriteLine();

} while (true);
