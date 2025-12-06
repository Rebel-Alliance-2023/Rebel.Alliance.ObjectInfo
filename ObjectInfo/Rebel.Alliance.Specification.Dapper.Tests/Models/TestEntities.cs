using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Models
{
    [Table("Customers")]
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Email { get; set; }

        public bool IsActive { get; set; }

        [Column("DateCreated")]
        public DateTime CreatedDate { get; set; }

        [Column("LastModified")]
        public DateTime? ModifiedDate { get; set; }

        public CustomerType CustomerType { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CreditLimit { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();

        [Column("PreferredContactMethod")]
        public ContactMethod PreferredContact { get; set; }

        [Column("MetaData", TypeName = "nvarchar(max)")]
        public string? MetaDataJson { get; set; }
    }

    [Table("Orders")]
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        public Customer Customer { get; set; } = null!;

        [ForeignKey("Customer")]
        public int CustomerId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public OrderStatus Status { get; set; }

        public DateTime OrderDate { get; set; }

        public DateTime? ShippedDate { get; set; }

        [MaxLength(500)]
        public string? ShippingAddress { get; set; }

        public bool IsPriority { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }

    [Table("OrderItems")]
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Order")]
        public int OrderId { get; set; }

        [ForeignKey("Product")]
        public int ProductId { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; }

        public bool IsGift { get; set; }

        [MaxLength(200)]
        public string? Notes { get; set; }
    }

    [Table("Products")]
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string SKU { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public bool IsAvailable { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public ProductCategory Category { get; set; }

        public int StockLevel { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Weight { get; set; }

        public string? TagsJson { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? LastRestockDate { get; set; }
    }


    [Table("Audits")]
    public class AuditLog
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string EntityName { get; set; } = string.Empty;

        public int EntityId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string UserId { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }

        [Column("OldValues", TypeName = "nvarchar(max)")]
        public string? OldValuesJson { get; set; }

        [Column("NewValues", TypeName = "nvarchar(max)")]
        public string? NewValuesJson { get; set; }
    }

    public enum CustomerType
    {
        Regular = 0,
        Premium = 1,
        VIP = 2,
        Wholesale = 3
    }

    public enum ContactMethod
    {
        Email = 0,
        Phone = 1,
        Mail = 2,
        None = 3
    }

    public enum OrderStatus
    {
        Pending = 0,
        Processing = 1,
        Shipped = 2,
        Delivered = 3,
        Cancelled = 4,
        Returned = 5
    }

    public enum ProductCategory
    {
        Electronics = 0,
        Clothing = 1,
        Books = 2,
        Food = 3,
        Home = 4,
        Sports = 5,
        Other = 6
    }

    public static class TestDataContants
    {
        public const string AuditSchema = "audit";
        public const int DefaultPageSize = 20;
        public const decimal MinimumOrderAmount = 10.00m;
        public const decimal StandardDiscountRate = 0.10m;
        public const decimal PremiumDiscountRate = 0.15m;
        public const decimal VipDiscountRate = 0.20m;
        public const int MinimumStockLevel = 10;
        public const int CriticalStockLevel = 5;
    }

    // DTOs for testing complex queries
    public class OrderSummaryDto
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
    }

    public class ProductStockDto
    {
        public string SKU { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public decimal Value { get; set; }
        public bool NeedsReorder => CurrentStock <= TestDataContants.MinimumStockLevel;
        public bool Critical => CurrentStock <= TestDataContants.CriticalStockLevel;
    }

    public class CustomerStatisticsDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime LastOrderDate { get; set; }
        public decimal AverageOrderValue { get; set; }
        public CustomerType Type { get; set; }
    }
}
