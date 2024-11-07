using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Bogus;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Models;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Infrastructure
{
    public static class TestDataGenerator
    {
        private static readonly Faker _faker = new();

        public static IEnumerable<Product> GenerateProducts(int count = 50)
        {
            var productFaker = new Faker<Product>()
                .RuleFor(p => p.Name, f => f.Commerce.ProductName())
                .RuleFor(p => p.SKU, f => f.Commerce.Ean13())
                .RuleFor(p => p.Price, f => Math.Round(f.Random.Decimal(10, 1000), 2))
                .RuleFor(p => p.IsAvailable, f => f.Random.Bool(0.8f))
                .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
                .RuleFor(p => p.Category, f => f.PickRandom<ProductCategory>())
                .RuleFor(p => p.StockLevel, f => f.Random.Int(0, 100))
                .RuleFor(p => p.Weight, f => Math.Round(f.Random.Decimal(0.1m, 50m), 2))
                .RuleFor(p => p.TagsJson, f => JsonSerializer.Serialize(f.Make(3, () => f.Commerce.ProductAdjective())))
                .RuleFor(p => p.CreatedDate, f => f.Date.Past(2))
                .RuleFor(p => p.LastRestockDate, f => f.Random.Bool(0.7f) ? f.Date.Past(1) : null);

            return productFaker.Generate(count);
        }

        public static IEnumerable<Customer> GenerateCustomers(int count = 20)
        {
            var customerFaker = new Faker<Customer>()
                .RuleFor(c => c.Name, f => f.Company.CompanyName())
                .RuleFor(c => c.Email, f => f.Internet.Email())
                .RuleFor(c => c.IsActive, f => f.Random.Bool(0.9f))
                .RuleFor(c => c.CreatedDate, f => f.Date.Past(3))
                .RuleFor(c => c.ModifiedDate, (f, c) => f.Date.Between(c.CreatedDate, DateTime.Now))
                .RuleFor(c => c.CustomerType, f => f.PickRandom<CustomerType>())
                .RuleFor(c => c.CreditLimit, f => Math.Round(f.Random.Decimal(1000, 50000), 2))
                .RuleFor(c => c.Notes, f => f.Random.Bool(0.7f) ? f.Lorem.Paragraph() : null)
                .RuleFor(c => c.PreferredContact, f => f.PickRandom<ContactMethod>())
                .RuleFor(c => c.MetaDataJson, f => JsonSerializer.Serialize(new
                {
                    LastContact = f.Date.Recent(),
                    Tags = f.Make(2, () => f.Commerce.Department()),
                    Rating = f.Random.Int(1, 5)
                }));

            return customerFaker.Generate(count);
        }

        public static IEnumerable<Order> GenerateOrders(IEnumerable<Customer> customers, int ordersPerCustomer = 5)
        {
            var customerIds = customers.Select(c => c.Id).ToList();
            var orderFaker = new Faker<Order>()
                .RuleFor(o => o.OrderNumber, f => f.Random.Replace("ORD-####-####"))
                .RuleFor(o => o.CustomerId, f => f.PickRandom(customerIds))
                .RuleFor(o => o.Status, f => f.PickRandom<OrderStatus>())
                .RuleFor(o => o.OrderDate, f => f.Date.Past(1))
                .RuleFor(o => o.ShippedDate, (f, o) => o.Status >= OrderStatus.Shipped ? f.Date.Between(o.OrderDate, DateTime.Now) : null)
                .RuleFor(o => o.ShippingAddress, f => f.Address.FullAddress())
                .RuleFor(o => o.IsPriority, f => f.Random.Bool(0.2f));

            var orders = new List<Order>();
            foreach (var customer in customers)
            {
                orders.AddRange(orderFaker.Generate(ordersPerCustomer));
            }

            return orders;
        }

        public static IEnumerable<OrderItem> GenerateOrderItems(int orderId, IEnumerable<Product> availableProducts)
        {
            var products = availableProducts.Where(p => p.IsAvailable).ToList();
            var itemCount = _faker.Random.Int(1, 5);
            var items = new List<OrderItem>();

            for (int i = 0; i < itemCount; i++)
            {
                var product = _faker.PickRandom(products);
                var quantity = _faker.Random.Int(1, 10);
                var unitPrice = product.Price;
                var isGift = _faker.Random.Bool(0.1f);
                var discount = isGift ? unitPrice : _faker.Random.Decimal(0, unitPrice * 0.3m);

                items.Add(new OrderItem
                {
                    OrderId = orderId,
                    ProductId = product.Id,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    Discount = Math.Round(discount, 2),
                    IsGift = isGift,
                    Notes = isGift ? "Gift Item" : null
                });
            }

            return items;
        }

        public static IEnumerable<AuditLog> GenerateAuditLogs(IEnumerable<Customer> customers, IEnumerable<Order> orders)
        {
            var auditLogs = new List<AuditLog>();
            var users = new[] { "system", "admin", "audit.service" };

            // Customer audit logs
            foreach (var customer in customers.Take(5))
            {
                auditLogs.Add(new AuditLog
                {
                    EntityName = "Customer",
                    EntityId = customer.Id,
                    Action = "Update",
                    UserId = _faker.PickRandom(users),
                    Timestamp = _faker.Date.Recent(),
                    OldValuesJson = JsonSerializer.Serialize(new { customer.CreditLimit, customer.CustomerType }),
                    NewValuesJson = JsonSerializer.Serialize(new
                    {
                        CreditLimit = customer.CreditLimit + 1000,
                        CustomerType = CustomerType.VIP
                    })
                });
            }

            // Order audit logs
            foreach (var order in orders.Take(10))
            {
                auditLogs.Add(new AuditLog
                {
                    EntityName = "Order",
                    EntityId = order.Id,
                    Action = "StatusChange",
                    UserId = _faker.PickRandom(users),
                    Timestamp = _faker.Date.Between(order.OrderDate, DateTime.Now),
                    OldValuesJson = JsonSerializer.Serialize(new { Status = OrderStatus.Processing }),
                    NewValuesJson = JsonSerializer.Serialize(new { Status = OrderStatus.Shipped })
                });
            }

            return auditLogs;
        }
    }
}
