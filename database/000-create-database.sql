-- Run against master. Existing databases are never dropped.
IF DB_ID(N'TaskManager') IS NULL
    CREATE DATABASE [TaskManager];
GO
