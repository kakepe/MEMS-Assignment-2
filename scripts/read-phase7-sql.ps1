# Read-only export: schema and public aggregate counts only; never export Identity row values.
$ErrorActionPreference='Stop'
$workspace=Split-Path -Parent $PSScriptRoot
Push-Location -LiteralPath $workspace
$connection=$null
try {
    $id=& docker inspect mems-assignment2-sql --format '{{.Id}}'
    if($LASTEXITCODE -ne 0 -or $id -ne 'e7fd02edfdaa499fb8c6ca179b91c3e680cd069787270457bdda608932ec7a26'){throw 'Unexpected SQL container; stopped.'}
    $ports=(& docker inspect mems-assignment2-sql --format '{{json .NetworkSettings.Ports}}') | ConvertFrom-Json
    $port=$ports.'1433/tcp'[0].HostPort
    if(-not $port){throw 'Existing container is not ready.'}
    $secret=Get-Content .local/apphost-secrets.json -Raw | ConvertFrom-Json
    $cs=New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $cs['Data Source']="127.0.0.1,$port";$cs['Initial Catalog']='MEMSAssignment2';$cs['User ID']='sa';$cs['Password']=$secret.Parameters.'mems-sql-password';$cs['Encrypt']=$true;$cs['TrustServerCertificate']=$true
    $connection=New-Object System.Data.SqlClient.SqlConnection($cs.ConnectionString);$connection.Open()
    function Rows([string]$sql){
        $cmd=$connection.CreateCommand();$cmd.CommandText=$sql;$reader=$cmd.ExecuteReader()
        try{while($reader.Read()){$row=[ordered]@{};for($i=0;$i -lt $reader.FieldCount;$i++){$row[$reader.GetName($i)]=if($reader.IsDBNull($i)){$null}else{$reader.GetValue($i)}};[pscustomobject]$row}}
        finally{$reader.Dispose();$cmd.Dispose()}
    }
    $columns=@(Rows 'SELECT t.name AS tableName,c.column_id AS ordinal,c.name AS columnName,ty.name AS sqlType,c.max_length AS maxLength,c.precision,c.scale,c.is_nullable AS nullable,c.is_identity AS identityColumn FROM sys.tables t JOIN sys.columns c ON t.object_id=c.object_id JOIN sys.types ty ON c.user_type_id=ty.user_type_id WHERE t.is_ms_shipped=0 ORDER BY t.name,c.column_id')
    $keys=@(Rows 'SELECT t.name AS tableName,i.name AS indexName,i.is_primary_key AS primaryKey,i.is_unique AS uniqueKey,c.name AS columnName,ic.key_ordinal AS keyOrdinal,i.filter_definition AS filterDefinition FROM sys.tables t JOIN sys.indexes i ON i.object_id=t.object_id JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE t.is_ms_shipped=0 AND ic.key_ordinal>0 ORDER BY t.name,i.name,ic.key_ordinal')
    $foreignKeys=@(Rows 'SELECT fk.name AS constraintName,pt.name AS dependentTable,pc.name AS dependentColumn,rt.name AS principalTable,rc.name AS principalColumn,fkc.constraint_column_id AS ordinal,fk.delete_referential_action_desc AS onDelete FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fkc ON fk.object_id=fkc.constraint_object_id JOIN sys.tables pt ON pt.object_id=fk.parent_object_id JOIN sys.columns pc ON pc.object_id=pt.object_id AND pc.column_id=fkc.parent_column_id JOIN sys.tables rt ON rt.object_id=fk.referenced_object_id JOIN sys.columns rc ON rc.object_id=rt.object_id AND rc.column_id=fkc.referenced_column_id ORDER BY fk.name,fkc.constraint_column_id')
    $counts=@(Rows 'SELECT (SELECT COUNT(*) FROM Municipalities) AS municipalities,(SELECT COUNT(*) FROM Positions) AS positions,(SELECT COUNT(*) FROM Candidates) AS candidates,(SELECT COUNT(*) FROM AspNetUsers) AS users,(SELECT COUNT(*) FROM AspNetRoles) AS roles')[0]
    $migrations=@(Rows 'SELECT MigrationId,ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId')
    $tables=@($columns.tableName | Sort-Object -Unique)
    if($tables.Count -ne 12 -or $counts.municipalities -ne 5 -or $counts.candidates -ne 30 -or $counts.users -ne 5 -or $migrations.Count -ne 2){throw 'Unexpected baseline schema/counts.'}
    [ordered]@{capturedAt=(Get-Date).ToString('o');database='MEMSAssignment2';containerId=$id;tables=$tables;counts=$counts;migrations=$migrations;columns=$columns;indexes=$keys;foreignKeys=$foreignKeys;provenance='Read-only live SQL metadata/count queries; no user/password/hash/token row values exported.'} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath docs/final/evidence/sql-snapshot.json -Encoding UTF8
    Write-Output 'Exported live SQL schema/count metadata for all 12 tables, without Identity row values.'
} finally{if($connection){$connection.Dispose()};Pop-Location}
