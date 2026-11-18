/*
    Least-privilege database user for the MCP server.

    Layer 1 of the security model: even if every check in the application failed,
    this user can only SELECT from the tables listed below. It cannot write,
    run procedures, or read SalesLT.Customer (which holds password hashes).

    Keep the GRANT list in sync with SqlMcp:AllowedTables in appsettings.json.
    Run in the AdventureWorksLT database. Use section A locally, section B on Azure.
*/

------------------------------------------------------------------------------
-- A. Local SQL Server: contained user with a password
------------------------------------------------------------------------------
-- Requires: EXEC sp_configure 'contained database authentication', 1; RECONFIGURE;
--           ALTER DATABASE AdventureWorksLT SET CONTAINMENT = PARTIAL;
-- (or create a server login and map it, if you prefer)

CREATE USER mcp_reader WITH PASSWORD = N'<same as MCP_READER_PASSWORD in .env>';
GO

------------------------------------------------------------------------------
-- B. Azure SQL: user for the Container App's managed identity (no password)
------------------------------------------------------------------------------
-- Connect as the Microsoft Entra admin of the server, then:
-- CREATE USER [<container-app-name>] FROM EXTERNAL PROVIDER;
-- and replace mcp_reader with [<container-app-name>] in the statements below.

------------------------------------------------------------------------------
-- Permissions (both cases)
------------------------------------------------------------------------------
-- Table by table, instead of db_datareader: new tables are not exposed by accident.
GRANT SELECT ON OBJECT::SalesLT.Product            TO mcp_reader;
GRANT SELECT ON OBJECT::SalesLT.ProductCategory    TO mcp_reader;
GRANT SELECT ON OBJECT::SalesLT.ProductModel       TO mcp_reader;
GRANT SELECT ON OBJECT::SalesLT.ProductDescription TO mcp_reader;
GRANT SELECT ON OBJECT::SalesLT.SalesOrderHeader   TO mcp_reader;
GRANT SELECT ON OBJECT::SalesLT.SalesOrderDetail   TO mcp_reader;

-- Belt and braces: explicit denies win over any role membership granted later.
DENY SELECT  ON OBJECT::SalesLT.Customer TO mcp_reader;
DENY EXECUTE TO mcp_reader;
DENY INSERT, UPDATE, DELETE, ALTER TO mcp_reader;
GO

-- Check: should list only SELECT grants plus the denies above.
SELECT  pr.name AS principal, pe.state_desc, pe.permission_name,
        OBJECT_SCHEMA_NAME(pe.major_id) + N'.' + OBJECT_NAME(pe.major_id) AS object_name
FROM    sys.database_permissions AS pe
JOIN    sys.database_principals  AS pr ON pr.principal_id = pe.grantee_principal_id
WHERE   pr.name = N'mcp_reader'
ORDER BY pe.state_desc, object_name;
