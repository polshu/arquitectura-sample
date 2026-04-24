# CLAUDE.md

Este archivo le da contexto a Claude Code (claude.ai/code) cuando trabaja en este proyecto.

## Contexto del proyecto

Proyecto educativo con alumnos de secundaria. Saben programar C# pero no arquitectura. El docente (Pablo) los esta guiando hacia una arquitectura por capas sencilla, sin DI / EF / AutoMapper / Dapper. El objetivo pedagogico es que lleguen a entender SOLID, DRY y KISS sin ahogarlos de abstraccion. Cuando escribas codigo nuevo aca, ponete en modo docente: ejemplos claros, nombres completos, sin cleverness, sin agregar capas que el alumno no pidio.

Esta carpeta es un **mini proyecto** recortado al minimo indispensable para mostrar la arquitectura. No hay auth, ni uploads, ni SignalR, ni Electron, ni legacy: solo el flujo Controller -> Service -> Repository -> DB. La version completa original vive en `../Indeura-main-backup/`.

## Commands

- Build: `dotnet build Indeura.sln`
- Run (dev): `dotnet run --project proyect.csproj` — escucha en `http://localhost:5042` (y `https://localhost:7010` con el profile `https` de `Properties/launchSettings.json`).
- Restore: `dotnet restore`
- No hay tests ni lint configurados.

## Arquitectura: Entity -> Service -> Repository -> DB (+ DTOs + Infrastructure)

```
Controller (Controllers/GamesController.cs)
      |
      v
Service (Services/GameService.cs, UserService.cs)                       <- reglas y orquestacion
      |               |
      |               +-> Infrastructure/Email/, Infrastructure/Steam/
      |                   EmailSenderFake, SteamInfoClient
      |                   <- clientes hacia sistemas externos (mail, APIs)
      v
Repository (Infrastructure/Persistence/Repositories/GameRepository.cs)  <- SQL y mapeo fila<->entidad
      |
      v
DBPostgres / DBMssql (Infrastructure/Persistence/)                      <- ADO.NET crudo
      |
      v
appsettings.json (seccion "Database")
```

DTOs viven en `DTOs/`:
- `DTOs/Input/` para DTOs de entrada (lo que manda el cliente al servidor).
- `DTOs/External/` para DTOs externos (lo que devuelven APIs de terceros como Steam).

La conversion DTO -> entidad se hace **a mano** dentro del Service, property por property.

Reglas que respetar cuando edites o extiendas:

- **Cada capa `new`ea la siguiente como field privado.** Sin contenedor, sin DI, sin factories. Ej: `private readonly GameRepository _repository = new GameRepository();`.
- **Ningun Controller habla con el Repository ni con la DB directamente.** Siempre pasa por el Service.
- **El Service orquesta entre entidades.** Si una operacion de Game necesita tocar al publisher, eso vive en `GameService` y llama a `UserService`, no lo hace el Controller (ver `GameService.GetPublisher`).
- **El Repository es el unico que escribe SQL.** Cualquier uso de stored procedure tambien va ahi.
- **Las entidades (`Models/Game.cs`, `Models/User.cs`) son POCOs puros.** No tienen metodos de negocio, no se auto-guardan. Viven en el namespace global (sin `namespace`) a proposito: la decision didactica es no mezclar namespaces hasta que los alumnos entiendan por que.
- **No introducir Dapper, EF, AutoMapper, ni ningun contenedor de DI.** El mapeo fila -> entidad vive en los helpers `BuildXFromRow` de cada Repository, property por property con `Convert.ToXxx`. Las clases DB no tienen reflection. Mismo criterio para DTO -> entidad: se copia campo por campo en el Service (ver `GameService.Create`).
- **Clientes externos viven en `Infrastructure/`** (Email, Steam, ...). Los usa el Service, NUNCA el Controller ni el Repository. Por defecto empezamos con una version "Fake" (escribe a consola, datos hardcoded) para que el proyecto corra sin credenciales; el dia que se conecta SMTP/HTTP reales, se cambia solo el cuerpo del metodo.

## Las dos clases DB (dos "sabores")

`Infrastructure/Persistence/DBPostgres.cs` y `Infrastructure/Persistence/DBMssql.cs` tienen **la misma API publica**:

- `List<Dictionary<string, object?>> Query(sql, parameters)` — devuelve todas las filas como diccionarios.
- `Dictionary<string, object?>? QueryFirst(sql, parameters)` — primera fila o null.
- `int Execute(sql, parameters)` — INSERT/UPDATE/DELETE, devuelve filas afectadas.
- `object? ExecuteScalar(sql, parameters)` — para COUNT o valor unico. El Repository convierte al tipo concreto con `Convert.ToXxx`.

Los `parameters` son `Dictionary<string, object>` (clave = nombre del parametro sin `@`, valor = lo que reemplaza a `@Nombre` en el SQL). Las filas vuelven como `Dictionary<string, object?>` con comparador case-insensitive en las claves, para que no importe si la base devolvio `GameName` o `gamename`.

**No hay genericos ni reflection en las clases DB.** Son tubos tontos de ADO.NET. Por diseno pedagogico: los alumnos ven paso a paso abrir conexion -> crear comando -> agregar parametros -> ejecutar -> leer filas.

**La SQL NO es portable entre motores** — Postgres usa `ILIKE`, comillas dobles para identificadores reservados y `LIMIT`; SQL Server usa `LIKE`, corchetes y `TOP`. Por eso cada Repository sabe contra que motor esta hablando. Si te piden agregar soporte SQL Server a un Repository que hoy usa Postgres, eso implica reescribir la SQL, no solo swapear la clase DB.

## Como arma un Repository los parametros y las entidades

Cada Repository tiene dos helpers privados que concentran el mapeo entidad <-> base:

- `BuildParametersFromX(X entity)` -> `Dictionary<string, object>` con cada propiedad que va al SQL. **No incluye `Id`** (auto-increment en Insert); para Update, el Id se agrega por fuera del helper.
- `BuildXFromRow(Dictionary<string, object?> row)` -> arma la entidad leyendo `row["ColumnName"]` y pasandolo por `Convert.ToXxx` al tipo de la propiedad.

Si una columna se renombra o se agrega/quita un campo a la entidad, esos dos metodos son los unicos puntos de cambio. Conversiones que conviene recordar:

- `Verified` se guarda como entero 0/1 en la base pero la entidad tiene `bool`. Se convierte ida y vuelta en los helpers de `UserRepository`.
- `COUNT(*)` en Postgres devuelve `long` (Int64). Los `CountByXxx` hacen `Convert.ToInt32(scalarResult)`.

## Patron pedagogico "reusabilidad vs COUNT" en `UserService`

En `UserService` hay dos metodos casi identicos que **estan a proposito distintos**:

- `UserNameAlreadyUsed(userName)` **reusa** `UserRepository.GetByUserName` y chequea `!= null`. No hay `CountByUserName` en el Repository.
- `EmailAlreadyUsed(email)` usa una query dedicada `UserRepository.CountByEmail(email) > 0`. **Tiene un comentario largo** que le propone al alumno pensar como seria el refactor (reusando `GetByEmail`) y que trade-offs hay: reusar codigo / mantener menos queries vs. costo de traer todas las columnas cuando solo queremos contar.

**No "unifiques" esos dos metodos ni borres el comentario de `EmailAlreadyUsed`.** La inconsistencia es la leccion: los alumnos ven los dos caminos lado a lado y discuten cual conviene en cada caso. Si en el futuro Pablo decide cerrar el debate, te lo va a pedir explicitamente.

## Config de las bases

`appsettings.json` tiene seccion `Database` con sub-secciones `Postgres` y `SqlServer`, cada una con su `ConnectionString`. Se lee desde `Infrastructure/Persistence/DBConfig.cs` (carga `appsettings.json` una sola vez con `Newtonsoft.Json.Linq`), **sin usar `IConfiguration`**, porque los alumnos no estan listos para DI.

## Doc adicional

`README.md` tiene la explicacion larga para el alumno: definicion detallada de cada capa, donde poner orquestadores (Services de nivel superior tipo `PurchaseService`), cuando introducir DTOs y la nomenclatura estandar de carpetas de la industria (Domain / Application / Infrastructure / Presentation) comparada contra la que usamos aca.
