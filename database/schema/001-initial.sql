CREATE TABLE dbo.Users (
    Id uniqueidentifier NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    Email nvarchar(254) NOT NULL,
    NormalizedEmail nvarchar(254) COLLATE Latin1_General_100_BIN2 NOT NULL,
    PasswordHash nvarchar(512) NOT NULL,
    CreatedAtUtc datetime2(7) NOT NULL,
    CONSTRAINT UQ_Users_NormalizedEmail UNIQUE (NormalizedEmail),
    CONSTRAINT CK_Users_Email CHECK (LEN(Email) > 0 AND LEN(NormalizedEmail) > 0),
    CONSTRAINT CK_Users_PasswordHash CHECK (LEN(PasswordHash) > 0)
);
CREATE TABLE dbo.Tasks (
    Id uniqueidentifier NOT NULL CONSTRAINT PK_Tasks PRIMARY KEY,
    UserId uniqueidentifier NOT NULL,
    Title nvarchar(120) NOT NULL,
    Description nvarchar(2000) NOT NULL,
    Status tinyint NOT NULL,
    DueDateUtc datetime2(7) NULL,
    CreatedAtUtc datetime2(7) NOT NULL,
    UpdatedAtUtc datetime2(7) NOT NULL,
    CONSTRAINT FK_Tasks_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_Tasks_Title CHECK (LEN(LTRIM(RTRIM(Title))) > 0),
    CONSTRAINT CK_Tasks_Status CHECK (Status IN (0,1,2))
);
CREATE INDEX IX_Tasks_UserId_CreatedAtUtc ON dbo.Tasks(UserId, CreatedAtUtc DESC, Id);
