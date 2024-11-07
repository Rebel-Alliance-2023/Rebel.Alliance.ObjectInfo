-- Create Tables
CREATE TABLE Customers (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Email TEXT,
    IsActive INTEGER NOT NULL,
    DateCreated TEXT NOT NULL,
    LastModified TEXT,
    CustomerType INTEGER NOT NULL,
    CreditLimit REAL NOT NULL,
    Notes TEXT,
    PreferredContactMethod INTEGER NOT NULL,
    MetaData TEXT
);

CREATE TABLE Products (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    SKU TEXT NOT NULL,
    Price REAL NOT NULL,
    IsAvailable INTEGER NOT NULL,
    Description TEXT,
    Category INTEGER NOT NULL,
    StockLevel INTEGER NOT NULL,
    Weight REAL,
    Tags TEXT,
    CreatedDate TEXT NOT NULL,
    LastRestockDate TEXT
);

CREATE TABLE Orders (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderNumber TEXT NOT NULL,
    CustomerId INTEGER NOT NULL,
    TotalAmount REAL NOT NULL,
    Status INTEGER NOT NULL,
    OrderDate TEXT NOT NULL,
    ShippedDate TEXT,
    ShippingAddress TEXT,
    IsPriority INTEGER NOT NULL,
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
);

CREATE TABLE OrderItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    Quantity INTEGER NOT NULL,
    UnitPrice REAL NOT NULL,
    Discount REAL NOT NULL,
    IsGift INTEGER NOT NULL,
    Notes TEXT,
    FOREIGN KEY (OrderId) REFERENCES Orders(Id),
    FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

CREATE TABLE Audits (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EntityName TEXT NOT NULL,
    EntityId INTEGER NOT NULL,
    Action TEXT NOT NULL,
    UserId TEXT NOT NULL,
    Timestamp TEXT NOT NULL,
    OldValues TEXT,
    NewValues TEXT
);

-- Create Indices
CREATE INDEX idx_customers_name ON Customers(Name);
CREATE INDEX idx_customers_email ON Customers(Email);
CREATE INDEX idx_customers_type ON Customers(CustomerType);
CREATE INDEX idx_customers_active ON Customers(IsActive);

CREATE INDEX idx_products_sku ON Products(SKU);
CREATE INDEX idx_products_category ON Products(Category);
CREATE INDEX idx_products_available ON Products(IsAvailable);
CREATE INDEX idx_products_stock ON Products(StockLevel);

CREATE INDEX idx_orders_number ON Orders(OrderNumber);
CREATE INDEX idx_orders_customer ON Orders(CustomerId);
CREATE INDEX idx_orders_status ON Orders(Status);
CREATE INDEX idx_orders_date ON Orders(OrderDate);

CREATE INDEX idx_order_items_order ON OrderItems(OrderId);
CREATE INDEX idx_order_items_product ON OrderItems(ProductId);

CREATE INDEX idx_audits_entity ON Audits(EntityName, EntityId);
CREATE INDEX idx_audits_timestamp ON Audits(Timestamp);
CREATE INDEX idx_audits_user ON Audits(UserId);

-- Create Views
CREATE VIEW vw_OrderSummary AS
SELECT 
    o.Id AS OrderId,
    o.OrderNumber,
    c.Name AS CustomerName,
    o.TotalAmount,
    COUNT(oi.Id) AS ItemCount,
    o.OrderDate,
    o.Status
FROM Orders o
JOIN Customers c ON o.CustomerId = c.Id
JOIN OrderItems oi ON o.Id = oi.OrderId
GROUP BY o.Id, o.OrderNumber, c.Name, o.TotalAmount, o.OrderDate, o.Status;

CREATE VIEW vw_ProductStock AS
SELECT
    p.Id,
    p.SKU,
    p.Name,
    p.StockLevel AS CurrentStock,
    p.Price * p.StockLevel AS Value,
    p.LastRestockDate
FROM Products p
WHERE p.IsAvailable = 1;

CREATE VIEW vw_CustomerStatistics AS
SELECT
    c.Id AS CustomerId,
    c.Name AS CustomerName,
    COUNT(DISTINCT o.Id) AS TotalOrders,
    SUM(o.TotalAmount) AS TotalSpent,
    MAX(o.OrderDate) AS LastOrderDate,
    AVG(o.TotalAmount) AS AverageOrderValue,
    c.CustomerType
FROM Customers c
LEFT JOIN Orders o ON c.Id = o.CustomerId
GROUP BY c.Id, c.Name, c.CustomerType;

-- Create Triggers
CREATE TRIGGER trg_Orders_UpdateTotal
AFTER INSERT ON OrderItems
BEGIN
    UPDATE Orders 
    SET TotalAmount = (
        SELECT SUM((oi.UnitPrice * oi.Quantity) - oi.Discount)
        FROM OrderItems oi
        WHERE oi.OrderId = NEW.OrderId
    )
    WHERE Id = NEW.OrderId;
END;

CREATE TRIGGER trg_Products_UpdateStock
AFTER INSERT ON OrderItems
BEGIN
    UPDATE Products
    SET StockLevel = StockLevel - NEW.Quantity
    WHERE Id = NEW.ProductId;
END;

CREATE TRIGGER trg_Customers_LastModified
AFTER UPDATE ON Customers
BEGIN
    UPDATE Customers
    SET LastModified = DATETIME('now')
    WHERE Id = NEW.Id;
END;
