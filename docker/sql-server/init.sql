-- Create Tables

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[RawEvents]') AND type in (N'U'))
BEGIN
CREATE TABLE [RawEvents] (
    [EventId] nvarchar(50) NOT NULL,
    [EventType] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [Payload] nvarchar(max) NOT NULL,
    [ProcessedAt] datetime2 NULL,
    CONSTRAINT [PK_RawEvents] PRIMARY KEY ([EventId])
);
CREATE INDEX [IX_RawEvents_ProcessedAt] ON [RawEvents] ([ProcessedAt]);
END;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[CurrentSubscriptions]') AND type in (N'U'))
BEGIN
CREATE TABLE [CurrentSubscriptions] (
    [SubscriptionId] nvarchar(50) NOT NULL,
    [CustomerId] nvarchar(50) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [CurrentProduct] nvarchar(50) NULL,
    [CurrentPrice] nvarchar(50) NULL,
    [CurrentQuantity] int NOT NULL,
    [CurrentAmount] decimal(18,2) NOT NULL,
    [Currency] nvarchar(3) NULL,
    [LastEventId] nvarchar(50) NULL,
    [LastUpdated] datetime2 NOT NULL,
    CONSTRAINT [PK_CurrentSubscriptions] PRIMARY KEY ([SubscriptionId])
);
END;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[SubscriptionHistory]') AND type in (N'U'))
BEGIN
CREATE TABLE [SubscriptionHistory] (
    [Id] int NOT NULL IDENTITY,
    [SubscriptionId] nvarchar(50) NOT NULL,
    [EventId] nvarchar(50) NOT NULL,
    [ChangeType] nvarchar(20) NOT NULL,
    [PreviousMRR] decimal(18,2) NOT NULL,
    [NewMRR] decimal(18,2) NOT NULL,
    [MRRDelta] decimal(18,2) NOT NULL,
    [Timestamp] datetime2 NOT NULL,
    CONSTRAINT [PK_SubscriptionHistory] PRIMARY KEY ([Id])
);
CREATE INDEX [IX_SubscriptionHistory_SubscriptionId] ON [SubscriptionHistory] ([SubscriptionId]);
CREATE INDEX [IX_SubscriptionHistory_Timestamp] ON [SubscriptionHistory] ([Timestamp]);
END;
GO
