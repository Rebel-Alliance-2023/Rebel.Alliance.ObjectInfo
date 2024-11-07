using Dapper;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Models;
using System.ComponentModel.DataAnnotations.Schema;

public static class DapperTypeMap
{
    public static void Configure()
    {
        // Register custom type maps
        var map = new CustomPropertyTypeMap(typeof(Order),
            (type, columnName) =>
                type.GetProperties().FirstOrDefault(prop =>
                    prop.GetCustomAttributes(false)
                        .OfType<ColumnAttribute>()
                        .Any(attr => attr.Name == columnName))!);

        SqlMapper.SetTypeMap(typeof(Order), map);
    }

    public static TResult MapOrder<TResult>(dynamic row) where TResult : class
    {
        var order = new Order
        {
            Id = row.Id,
            OrderNumber = row.OrderNumber,
            CustomerId = row.CustomerId,
            Customer = new Customer
            {
                Id = row.Customer_Id,
                Name = row.Customer_Name,
                CustomerType = (CustomerType)row.Customer_Type
            },
            Status = (OrderStatus)row.Status,
            OrderDate = row.OrderDate,
            ShippedDate = row.ShippedDate,
            ShippingAddress = row.ShippingAddress
        };
        return order as TResult;
    }
}
