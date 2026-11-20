# SQL Server data dictionary

Live read-only metadata: MEMSAssignment2; captured 2026-11-17T21:31:33.4156976-08:00. No Identity row values are exported.

The domain and Identity/history ERDs cover all twelve tables; the following dictionary lists actual columns and keys. `timestamp` is SQL Server row-version semantics, not a date/time.

## __EFMigrationsHistory

| Column | SQL type | Nullable | Keys |
| --- | --- | --- | --- |
| MigrationId | nvarchar(150) | No | PK: PK___EFMigrationsHistory |
| ProductVersion | nvarchar(32) | No | — |

## AspNetRoleClaims

| Column | SQL type | Nullable | Keys |
| --- | --- | --- | --- |
| Id | int | No | IDENTITY; PK: PK_AspNetRoleClaims |
| RoleId | nvarchar(128) | No | FK → AspNetRoles.Id |
| ClaimType | nvarchar(max) | Yes | — |
| ClaimValue | nvarchar(max) | Yes | — |

## AspNetRoles

| Column | SQL type | Nullable | Keys |
| --- | --- | --- | --- |
| Id | nvarchar(128) | No | UQ/AK: AK_AspNetRoles_Id_Scope; PK: PK_AspNetRoles |
| Scope | int | No | UQ/AK: AK_AspNetRoles_Id_Scope |
| MunicipalityId | int | Yes | FK → Municipalities.MunicipalityId |
| Name | nvarchar(256) | Yes | — |
| NormalizedName | nvarchar(256) | Yes | UQ/AK: RoleNameIndex |
| ConcurrencyStamp | nvarchar(max) | Yes | — |

## AspNetUserClaims

| Column | SQL type | Nullable | Keys |
| --- | --- | --- | --- |
| Id | int | No | IDENTITY; PK: PK_AspNetUserClaims |
| UserId | nvarchar(128) | No | FK → AspNetUsers.Id |
| ClaimType | nvarchar(max) | Yes | — |
| ClaimValue | nvarchar(max) | Yes | — |

## AspNetUserLogins

| Column | SQL type | Nullable | Keys |
| --- | --- | --- | --- |
| LoginProvider | nvarchar(128) | No | PK: PK_AspNetUserLogins |
| ProviderKey | nvarchar(128) | No | PK: PK_AspNetUserLogins |
| ProviderDisplayName | nvarchar(max) | Yes | — |
| UserId | nvarchar(128) | No | FK → AspNetUsers.Id |

## AspNetUserRoles

| Column | SQL type | Nullable | Keys |
| --- | --- | --- | --- |
| UserId | nvarchar(128) | No | PK: PK_AspNetUserRoles; FK → AspNetUsers.Id |
| RoleId | nvarchar(128) | No | PK: PK_AspNetUserRoles; FK → AspNetRoles.Id |
| Scope | int | No | FK → AspNetRoles.Scope; FK → AspNetUsers.Scope |

## AspNetUsers

| Column | SQL type | Nullable | Keys |
| --- | --- | --- | --- |
| Id | nvarchar(128) | No | UQ/AK: AK_AspNetUsers_Id_Scope; PK: PK_AspNetUsers |
| Scope | int | No | UQ/AK: AK_AspNetUsers_Id_Scope |
| MunicipalityId | int | Yes | FK → Municipalities.MunicipalityId |
| UserName | nvarchar(256) | Yes | — |
| NormalizedUserName | nvarchar(256) | Yes | UQ/AK: UserNameIndex |
| Email | nvarchar(256) | Yes | — |
| NormalizedEmail | nvarchar(256) | Yes | — |
| EmailConfirmed | bit | No | — |
| PasswordHash | nvarchar(max) | Yes | — |
| SecurityStamp | nvarchar(max) | Yes | — |
| ConcurrencyStamp | nvarchar(max) | Yes | — |
| PhoneNumber | nvarchar(max) | Yes | — |
| PhoneNumberConfirmed | bit | No | — |
| TwoFactorEnabled | bit | No | — |
| LockoutEnd | datetimeoffset | Yes | — |
| LockoutEnabled | bit | No | — |
| AccessFailedCount | int | No | — |

## AspNetUserTokens

| Column | SQL type | Nullable | Keys |
| --- | --- | --- | --- |
| UserId | nvarchar(128) | No | PK: PK_AspNetUserTokens; FK → AspNetUsers.Id |
| LoginProvider | nvarchar(128) | No | PK: PK_AspNetUserTokens |
| Name | nvarchar(128) | No | PK: PK_AspNetUserTokens |
| Value | nvarchar(max) | Yes | — |

## Candidates

| Column | SQL type | Nullable | Keys |
| --- | --- | --- | --- |
| CandidateId | int | No | IDENTITY; PK: PK_Candidates |
| MunicipalityId | int | No | FK → Municipalities.MunicipalityId; FK → Positions.MunicipalityId |
| PositionId | int | No | FK → Positions.PositionId |
| DemoSeedKey | nvarchar(80) | Yes | UQ/AK: IX_Candidates_DemoSeedKey |
| Name | nvarchar(100) | No | — |
| Profile | nvarchar(4000) | No | — |
| PhotoFileName | nvarchar(160) | Yes | — |
| Website | nvarchar(300) | Yes | — |
| Email | nvarchar(254) | Yes | — |
| DateCreated | datetime2 | No | — |
| RowVersion | timestamp | No | — |

## Municipalities

| Column | SQL type | Nullable | Keys |
| --- | --- | --- | --- |
| MunicipalityId | int | No | IDENTITY; PK: PK_Municipalities |
| Slug | nvarchar(80) | No | UQ/AK: IX_Municipalities_Slug |
| Name | nvarchar(100) | No | UQ/AK: IX_Municipalities_Name |
| Province | nvarchar(50) | No | — |
| ElectionDate | date | No | — |
| Description | nvarchar(2000) | No | — |
| LogoFileName | nvarchar(160) | Yes | — |
| BrandColor | nvarchar(7) | No | — |
| PublicTitle | nvarchar(120) | No | — |
| RowVersion | timestamp | No | — |

## Positions

| Column | SQL type | Nullable | Keys |
| --- | --- | --- | --- |
| PositionId | int | No | IDENTITY; UQ/AK: AK_Positions_MunicipalityId_PositionId; PK: PK_Positions |
| MunicipalityId | int | No | UQ/AK: AK_Positions_MunicipalityId_PositionId; UQ/AK: IX_Positions_MunicipalityId_Name; FK → Municipalities.MunicipalityId |
| Name | nvarchar(100) | No | UQ/AK: IX_Positions_MunicipalityId_Name |
| SeatsAvailable | int | No | — |
| RowVersion | timestamp | No | — |

## SeedHistory

| Column | SQL type | Nullable | Keys |
| --- | --- | --- | --- |
| SeedKey | nvarchar(80) | No | PK: PK_SeedHistory |
| CompletedAtUtc | datetime2 | No | — |

