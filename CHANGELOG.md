# Change Log

All notable changes to this project will be documented in this file. See [versionize](https://github.com/versionize/versionize) for commit guidelines.

<a name="2.3.15"></a>
## 2.3.15 (2025-02-21)

### Bug Fixes

* **database:** upgrade core packages

<a name="2.3.14"></a>
## 2.3.14 (2025-02-14)

### Bug Fixes

* **database:** fix use database not working

<a name="2.3.13"></a>
## 2.3.13 (2025-02-14)

### Bug Fixes

* **database:** added cache for connection strings and fixed open connection with use database
* **database:** upgraded core packages

<a name="2.3.12"></a>
## 2.3.12 (2025-02-13)

### Bug Fixes

* **databaseservice:** added a use database in the sql service

<a name="2.3.11"></a>
## 2.3.11 (2025-02-11)

### Bug Fixes

* **rbac:** rbac throws .net framework exceptions instead of error from scimv2

<a name="2.3.10"></a>
## 2.3.10 (2025-01-16)

### Bug Fixes

* **logs:** database log was leaking password when not in dev mode

<a name="2.3.9"></a>
## 2.3.9 (2025-01-16)

### Bug Fixes

* **healthcheck:** routing database was not disposing connection
* **logs:** fix connection string in logger message in database provider

<a name="2.3.8"></a>
## 2.3.8 (2025-01-16)

### Bug Fixes

* **sqldatabaseprovider:** fix error in provider

<a name="2.3.7"></a>
## 2.3.7 (2025-01-16)

### Bug Fixes

* **dependencies:** update middleware and core packages

<a name="2.3.6"></a>
## 2.3.6 (2025-01-16)

<a name="2.3.5"></a>
## 2.3.5 (2025-01-16)

### Bug Fixes

* **redis:** fix health check for redis service
* **sqlservice:** added routing database health check

<a name="2.3.4"></a>
## 2.3.4 (2025-01-16)

### Bug Fixes

* **services:** fixed wrong route value key in services

<a name="2.3.3"></a>
## 2.3.3 (2025-01-15)

### Bug Fixes

* **dependencies:** upgrade nuget packages

<a name="2.3.2"></a>
## 2.3.2 (2025-01-15)

### Bug Fixes

* **dependencies:** upgrade nuget packages
* **di:** added missing rbacservice registration

<a name="2.3.1"></a>
## 2.3.1 (2025-01-15)

### Bug Fixes

* **rbac:** add missing extension method to configure rbac on dep container

<a name="2.3.0"></a>
## 2.3.0 (2025-01-15)

### Features

* **rbac:** added rbac service implementation
* **sdk:** upgrade to .net 9

### Bug Fixes

* **dependencies:** upgraded nuget packages and removed redundant packages

<a name="2.2.8"></a>
## 2.2.8 (2025-01-14)

### Bug Fixes

* upgrade middleware packages
* upgrade packages

<a name="2.2.7"></a>
## 2.2.7 (2025-01-07)

<a name="2.2.6"></a>
## 2.2.6 (2025-01-02)

### Bug Fixes

* upgrade middleware packages

<a name="2.2.5"></a>
## 2.2.5 (2024-12-18)

### Bug Fixes

* **redisservice:** laz load database for redis service

<a name="2.2.4"></a>
## 2.2.4 (2024-12-06)

<a name="2.2.3"></a>
## 2.2.3 (2024-12-06)

<a name="2.2.2"></a>
## 2.2.2 (2024-12-06)

<a name="2.2.1"></a>
## 2.2.1 (2024-12-06)

<a name="2.2.0"></a>
## 2.2.0 (2024-12-04)

### Features

* added services for redis and sqldatabase (with connection provider per domain)

### Bug Fixes

* upgrade middleware packages

<a name="2.1.2"></a>
## 2.1.2 (2024-11-27)

<a name="2.1.1"></a>
## 2.1.1 (2024-11-27)

### Bug Fixes

* **services:** upgrade middleware services and fixed services and tests

<a name="2.1.0"></a>
## 2.1.0 (2024-11-12)

### Features

* **crudservices:** upgrade core and middleware packages

<a name="2.0.2"></a>
## 2.0.2 (2024-09-24)

### Bug Fixes

* fixed get apikey by clientid and secret

<a name="2.0.1"></a>
## 2.0.1 (2024-09-19)

### Bug Fixes

* upgrade middleware packages

<a name="2.0.0"></a>
## 2.0.0 (2024-09-17)

### Features

* rename client to apikey and added bcrypt digest

### Breaking Changes

* rename client to apikey and added bcrypt digest

<a name="1.1.7"></a>
## 1.1.7 (2024-09-12)

### Bug Fixes

* upgrade packages

<a name="1.1.6"></a>
## 1.1.6 (2024-09-11)

### Bug Fixes

* upgrade core and middleware packages

<a name="1.1.5"></a>
## 1.1.5 (2024-09-10)

### Bug Fixes

* upgrade middleware packages

<a name="1.1.4"></a>
## 1.1.4 (2024-09-09)

### Bug Fixes

* make use of observableproxy on patch

<a name="1.1.3"></a>
## 1.1.3 (2024-09-02)

### Bug Fixes

* update packages and fixed tests

<a name="1.1.2"></a>
## 1.1.2 (2024-08-22)

### Bug Fixes

* upgraded middleware packages

<a name="1.1.1"></a>
## 1.1.1 (2024-08-22)

### Bug Fixes

* patch and delete service methods get the entity by calling the getbyid service method
* upgrade middleware and test packages

<a name="1.1.0"></a>
## 1.1.0 (2024-08-22)

### Features

* added in memory implementation of patch operation and unit test for the services

<a name="1.0.11"></a>
## 1.0.11 (2024-07-20)

### Bug Fixes

* use body id to create entities

<a name="1.0.10"></a>
## 1.0.10 (2024-07-20)

### Bug Fixes

* upgrade packages

<a name="1.0.9"></a>
## 1.0.9 (2024-07-20)

### Bug Fixes

* upgrade middlewares to 1.0.19

<a name="1.0.8"></a>
## 1.0.8 (2024-07-19)

### Bug Fixes

* upgraded looplex packages

<a name="1.0.7"></a>
## 1.0.7 (2024-07-18)

### Bug Fixes

* upgrade middlewares to 1.0.14

<a name="1.0.6"></a>
## [1.0.6](https://www.github.com/looplex-osi/services-dotnet/releases/tag/v1.0.6) (2024-07-11)

### Bug Fixes

* fixed wrong id Guid parsing ([34378e3](https://www.github.com/looplex-osi/services-dotnet/commit/34378e34ceb52108d78f102b90e3d2aecedaf729))

<a name="1.0.5"></a>
## [1.0.5](https://www.github.com/looplex-osi/services-dotnet/releases/tag/v1.0.5) (2024-07-11)

### Bug Fixes

* upgrade middleware pkg to 1.0.11 ([bcb1073](https://www.github.com/looplex-osi/services-dotnet/commit/bcb1073443f32b41d1a807b5efd87c6bdb456b7d))

<a name="1.0.4"></a>
## [1.0.4](https://www.github.com/looplex-osi/services-dotnet/releases/tag/v1.0.4) (2024-07-11)

<a name="1.0.3"></a>
## [1.0.3](https://www.github.com/looplex-osi/services-dotnet/releases/tag/v1.0.3) (2024-07-11)

### Bug Fixes

* upgrade middleware pkg to 1.0.9 ([e0a7a5c](https://www.github.com/looplex-osi/services-dotnet/commit/e0a7a5cf1155776e523b47b740ce8517ed0be689))

<a name="1.0.2"></a>
## [1.0.2](https://www.github.com/looplex-osi/services-dotnet/releases/tag/v1.0.2) (2024-07-11)

### Bug Fixes

* upgrade middlewares to 1.0.8 ([2b6e798](https://www.github.com/looplex-osi/services-dotnet/commit/2b6e798ccd804b4d9df2b51db75306d5494c2899))

<a name="1.0.1"></a>
## [1.0.1](https://www.github.com/looplex-osi/services-dotnet/releases/tag/v1.0.1) (2024-07-08)

### Bug Fixes

* upgrade middlewares to 1.0.5 ([35d89b1](https://www.github.com/looplex-osi/services-dotnet/commit/35d89b1ebacfa23b18f8d3d9c772666043e527cd))

<a name="1.0.0"></a>
## [1.0.0](https://www.github.com/looplex-osi/services-dotnet/releases/tag/v1.0.0) (2024-07-08)

