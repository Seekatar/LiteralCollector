USE CodeAnalysis;
GO

IF OBJECT_ID('dbo.LITERAL', 'U') IS NULL
    CREATE TABLE dbo.LITERAL (
        LITERAL_ID INTEGER PRIMARY KEY,
        LITERAL VARCHAR(1000) NOT NULL
    );
ELSE
    print 'Table dbo.LITERAL already exists';

IF OBJECT_ID('dbo.LITERAL_LOCATION', 'U') IS NULL
    CREATE TABLE dbo.LITERAL_LOCATION (
        LITERAL_ID INTEGER NOT NULL,
        SOURCE_FILE_ID INTEGER NOT NULL,
        LINE INTEGER NOT NULL,
        CHARACTER INTEGER NOT NULL,
        IS_CONSTANT BIT NOT NULL,
        CONSTRAINT PK_LITERAL_LOCATION PRIMARY KEY (LITERAL_ID),
        CONSTRAINT FK_LOCCATION_LITERAL FOREIGN KEY (LITERAL_ID) REFERENCES LITERAL (LITERAL_ID),
        CONSTRAINT FK_LOCATION_SOURCE_FILE FOREIGN KEY (SOURCE_FILE_ID) REFERENCES SOURCE_FILE (SOURCE_FILE_ID)
    );
ELSE
    print 'Table dbo.LITERAL_LOCATION already exists';

IF OBJECT_ID('dbo.SOURCE_FILE', 'U') IS NULL
    CREATE TABLE dbo.SOURCE_FILE (
        SOURCE_FILE_ID INTEGER IDENTITY PRIMARY KEY,
        FILE_NAME VARCHAR(1000) NOT NULL
    );
ELSE
    print 'Table dbo.SOURCE_FILE already exists';