# 🏦 BHD Documents

API de **gateway interno para la gestión de documentos**, desarrollada en **.NET 8** bajo una **arquitectura hexagonal**.

---

## 🛠️ Tecnologías y Herramientas

- **.NET 8 Web API**
- **SQL Server 2022**
- **Docker & Docker Compose**
- **Arquitectura Hexagonal**
- **IDE:** Rider

---

## 🚀 Ejecución del Proyecto

### 📦 Requisitos Previos

Asegúrate de tener instalados:

- Docker Desktop
- .NET 8 SDK
- SQL Server 2022 (local o en contenedor)

---
## Clonar repositorio
```bash
https://github.com/HectorSosa19/BHD-Documents-Hexagonal-Architecture.git
```

## 🐳 Ejecución con Docker (Recomendado)

### Levantar los servicios
```bash
docker-compose up
Para visualizar los endpoints / API / Ir a la ruta o link.
http://localhost:5001/swagger/index.html

# Recomendacion:
Para bajar la imagen de docker-Desktop
docker-compose down
```

## Local / MacOS: 
```bash
Imagen de docker Sql Server 2022
docker run -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=SqlEdge@12345 -p 1433:1433 --name sql-edge -d mcr.microsoft.com/azure-sql-edge
docker ps --> Para verificar que esta arriba.
Visual Studio Code instalar la extension de mssql 
Registrar lo usuario y contraseña
Authentication type: SQL Login 
Usuario: sa
Password: SqlEdge@12345

Trust server certificate:True
Nombre Database: 
SQLSERVER2022
```
## Local / Windows:
```bash

Descargar SQL Server
Authentication type: SQL Login 
Usuario: sa
Password: SqlEdge@12345
```


## Code First:
```bash
dotnet dotnet-ef migrations add Initial
dotnet dotnet-ef database update 
```

## Documentación de Postman:
```bash
https://www.postman.com/spaceflight-geoscientist-12704151/bhd-api-documentation-postman/request/17947487-6943b6df-cfac-4209-b7e5-df3317bb1871
```


