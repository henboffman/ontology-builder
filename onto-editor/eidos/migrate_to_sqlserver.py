#!/usr/bin/env python3
"""
Convert SQLite dump to SQL Server format and import data
"""
import re
import subprocess

def convert_sqlite_to_sqlserver(input_file, output_file):
    """Convert SQLite SQL dump to SQL Server compatible format"""

    with open(input_file, 'r') as f:
        content = f.read()

    # Remove SQLite-specific commands
    content = re.sub(r'PRAGMA.*?;', '', content)
    content = re.sub(r'BEGIN TRANSACTION;', '', content)
    content = re.sub(r'COMMIT;', '', content)
    content = re.sub(r'CREATE TABLE IF NOT EXISTS', 'IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N\'[dbo].[{table}]\') AND type in (N\'U\'))\nCREATE TABLE', content)

    # Remove CREATE TABLE statements (we already have the schema from migrations)
    content = re.sub(r'CREATE TABLE.*?;', '', content, flags=re.DOTALL)
    content = re.sub(r'CREATE INDEX.*?;', '', content)
    content = re.sub(r'CREATE UNIQUE INDEX.*?;', '', content)

    # Convert INSERT statements
    # SQLite: INSERT INTO TableName VALUES(...)
    # SQL Server: INSERT INTO [TableName] VALUES(...)
    content = re.sub(r'INSERT INTO (\w+)', r'INSERT INTO [\1]', content)

    # Convert boolean values
    content = content.replace("'0'", '0')
    content = content.replace("'1'", '1')

    # Convert TEXT NULLs to proper NULLs
    content = content.replace("''", "NULL")

    # Add GO statements after each batch
    lines = content.split('\n')
    output_lines = []
    for line in lines:
        line = line.strip()
        if line and not line.startswith('--'):
            output_lines.append(line)
            if line.endswith(';'):
                output_lines.append('GO')

    with open(output_file, 'w') as f:
        f.write('\n'.join(output_lines))

    print(f"✅ Converted SQL dump saved to {output_file}")

def import_to_sqlserver(sql_file):
    """Import SQL file into SQL Server using sqlcmd"""
    print(f"\n🔄 Importing data into SQL Server...")

    # Use docker exec to run sqlcmd inside the container
    cmd = [
        'docker', 'exec', '-i', 'eidos-sqlserver',
        '/opt/mssql-tools18/bin/sqlcmd',
        '-S', 'localhost',
        '-U', 'sa',
        '-P', 'YourStrong!Passw0rd',
        '-d', 'EidosDb',
        '-C',  # Trust server certificate
        '-i', f'/tmp/{sql_file}'
    ]

    # First copy the file into the container
    subprocess.run(['docker', 'cp', sql_file, f'eidos-sqlserver:/tmp/{sql_file}'], check=True)

    result = subprocess.run(cmd, capture_output=True, text=True)

    if result.returncode == 0:
        print("✅ Data imported successfully!")
    else:
        print(f"❌ Import failed: {result.stderr}")
        return False

    return True

if __name__ == '__main__':
    print("🚀 Starting SQLite to SQL Server migration...\n")

    # Convert the dump
    convert_sqlite_to_sqlserver('sqlitedata.sql', 'sqlserver_import.sql')

    print("\n⚠️  Manual import required!")
    print("The SQL file has been converted to: sqlserver_import.sql")
    print("\nTo import manually, you can use one of these methods:")
    print("1. Azure Data Studio")
    print("2. SQL Server Management Studio")
    print("3. Or run the import statements from the converted file")
