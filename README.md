# AlwaysDecrypted

Tired of working with encrypted columns in your SQL Server database? AlwaysDecrypted is the tool that helps you decrypt data that is encrypted using _SQL Server Always Encrypted_.

## Usage

Download and run the application AlwaysDecrypted.exe.

Provide the name of your SQL Server and database like this (the server will default to localhost)

`-server=myserver -database=mydatabase`

The application will decrypt any data that is encrypted using _SQL Server Always Encrypted_ in the given database.

## Motivation

[SQL Server's column encryption feature](https://docs.microsoft.com/en-us/sql/relational-databases/security/encryption/always-encrypted-database-engine?view=sql-server-2017) aka _Always Encrypted_ was introduced as a way to protect sensitive data. It's now used in many places and companies, often because of the General Data Protection Regulation (GDPR).

Unfortunately it's often a pain to work with - for several reasons.

.Net Core has lacked support for column encryption in its Microsoft.Data.SqlClient up until the recent [preview of .NET Core 3.0](https://devblogs.microsoft.com/dotnet/announcing-net-core-3-0-preview-5/).

All scripts that are to be executed towards a database with encrypted columns have to be written carefully to avoid invalid operations. Often developers will end up with confusing errors like this

```error
The data types nvarchar(200) encrypted with (encryption_type = 'RANDOMIZED', encryption_algorithm_name = 'AEAD_AES_256_CBC_HMAC_SHA_256', column_encryption_key_name = 'CEK_Auto1', column_encryption_key_database_name = 'AlwaysDecrypted') and varchar are incompatible in the equal to operator.
```

All existing queries that used to execute just fine may now be broken because of column encryption. This could be reports, queries from an application, etc.

If you ended up with an encrypted database, but you've changed your mind and need a way out - then here it is.

## Technical notes

The application assumes that it is executed by a user that has access to the column master key(s) used to encrypt/decrypt the encrypted columns in your database.

The application reads all encrypted data into memory, decrypts it, and inserts it back into the database. All inserts are performed as bulk inserts which makes the decryption fairly efficient even against larger databases.

It is currently required that any tables containing encrypted columns also contain a primary key. Otherwise the application cannot decrypt the data.
