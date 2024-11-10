-- Create Tables with proper order considering foreign keys
CREATE TABLE IF NOT EXISTS Products (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    SKU TEXT NOT NULL UNIQUE,
    Price REAL NOT NULL,
    IsAvailable INTEGER NOT NULL,
    Description TEXT,
    Category INTEGER NOT NULL,
    StockLevel INTEGER NOT NULL,
    Weight REAL,
    TagsJson TEXT,
    CreatedDate TEXT NOT NULL,
    LastRestockDate TEXT
);


CREATE TABLE IF NOT EXISTS Customers (
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

CREATE TABLE IF NOT EXISTS Orders (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderNumber TEXT NOT NULL UNIQUE,
    CustomerId INTEGER NOT NULL,
    TotalAmount REAL NOT NULL,
    Status INTEGER NOT NULL,
    OrderDate TEXT NOT NULL,
    ShippedDate TEXT,
    ShippingAddress TEXT,
    IsPriority INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
);

CREATE TABLE IF NOT EXISTS OrderItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    Quantity INTEGER NOT NULL,
    UnitPrice REAL NOT NULL,
    Discount REAL NOT NULL DEFAULT 0,
    IsGift INTEGER NOT NULL DEFAULT 0,
    Notes TEXT,
    FOREIGN KEY (OrderId) REFERENCES Orders(Id),
    FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

CREATE TABLE IF NOT EXISTS Audits (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EntityName TEXT NOT NULL,
    EntityId INTEGER NOT NULL,
    Action TEXT NOT NULL,
    UserId TEXT NOT NULL,
    Timestamp TEXT NOT NULL,
    OldValues TEXT,
    NewValues TEXT
);

-- Create Indices (SQLite syntax)
CREATE INDEX IF NOT EXISTS idx_customers_name ON Customers(Name);
CREATE INDEX IF NOT EXISTS idx_customers_email ON Customers(Email);
CREATE INDEX IF NOT EXISTS idx_customers_type ON Customers(CustomerType);
CREATE INDEX IF NOT EXISTS idx_customers_active ON Customers(IsActive);

CREATE INDEX IF NOT EXISTS idx_products_sku ON Products(SKU);
CREATE INDEX IF NOT EXISTS idx_products_category ON Products(Category);
CREATE INDEX IF NOT EXISTS idx_products_available ON Products(IsAvailable);
CREATE INDEX IF NOT EXISTS idx_products_stock ON Products(StockLevel);

CREATE INDEX IF NOT EXISTS idx_orders_number ON Orders(OrderNumber);
CREATE INDEX IF NOT EXISTS idx_orders_customer ON Orders(CustomerId);
CREATE INDEX IF NOT EXISTS idx_orders_status ON Orders(Status);
CREATE INDEX IF NOT EXISTS idx_orders_date ON Orders(OrderDate);

CREATE INDEX IF NOT EXISTS idx_order_items_order ON OrderItems(OrderId);
CREATE INDEX IF NOT EXISTS idx_order_items_product ON OrderItems(ProductId);

CREATE INDEX IF NOT EXISTS idx_audits_entity ON Audits(EntityName, EntityId);
CREATE INDEX IF NOT EXISTS idx_audits_timestamp ON Audits(Timestamp);
CREATE INDEX IF NOT EXISTS idx_audits_user ON Audits(UserId);

-- Create Views
CREATE VIEW IF NOT EXISTS vw_OrderSummary AS
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

CREATE VIEW IF NOT EXISTS vw_ProductStock AS
SELECT
    p.Id,
    p.SKU,
    p.Name,
    p.StockLevel AS CurrentStock,
    p.Price * p.StockLevel AS Value,
    p.LastRestockDate
FROM Products p
WHERE p.IsAvailable = 1;

CREATE VIEW IF NOT EXISTS vw_CustomerStatistics AS
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

-- Triggers (SQLite syntax)
DROP TRIGGER IF EXISTS trg_Orders_UpdateTotal;
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

DROP TRIGGER IF EXISTS trg_Products_UpdateStock;
CREATE TRIGGER trg_Products_UpdateStock
AFTER INSERT ON OrderItems
BEGIN
    UPDATE Products
    SET StockLevel = StockLevel - NEW.Quantity
    WHERE Id = NEW.ProductId;
END;

DROP TRIGGER IF EXISTS trg_Customers_LastModified;
CREATE TRIGGER trg_Customers_LastModified
AFTER UPDATE ON Customers
BEGIN
    UPDATE Customers
    SET LastModified = datetime('now')
    WHERE Id = NEW.Id;
END;