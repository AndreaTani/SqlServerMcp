-- Local only: restores AdventureWorksLT from the backup mounted by docker-compose.
-- On Azure SQL, create the database with "Use existing data: Sample" instead.
-- Run as sa. If the MOVE names do not match, check the logical file names with:
--   RESTORE FILELISTONLY FROM DISK = N'/var/opt/mssql/backup/AdventureWorksLT2022.bak';

RESTORE DATABASE AdventureWorksLT
FROM DISK = N'/var/opt/mssql/backup/AdventureWorksLT2022.bak'
WITH
    MOVE N'AdventureWorksLT2022_Data' TO N'/var/opt/mssql/data/AdventureWorksLT.mdf',
    MOVE N'AdventureWorksLT2022_Log'  TO N'/var/opt/mssql/data/AdventureWorksLT_log.ldf',
    REPLACE;
GO
