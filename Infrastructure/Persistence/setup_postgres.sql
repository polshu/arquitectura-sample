-- =============================================================================
-- Script de creacion y populado de la base Indeura para Postgres (Supabase).
--
-- Como usarlo:
--   1) Entrar al dashboard de Supabase -> SQL Editor -> New query.
--   2) Pegar TODO este archivo y apretar Run.
--   3) Verificar con: SELECT * FROM "User";  SELECT * FROM Game;
--
-- Notas para el alumno:
--   - La tabla "User" lleva comillas dobles porque User es palabra reservada
--     en Postgres. Los nombres de columna no son reservados, asi que van sin
--     comillas. Postgres internamente los guarda en minuscula ("email",
--     "gamename", etc.), y eso esta bien: la app hace las queries sin
--     comillas tambien, asi que coinciden.
--   - SERIAL es la forma vieja y simple de hacer auto-increment en Postgres.
--     La base genera el Id sola en cada INSERT.
--   - REAL es float de 4 bytes, que matchea con el "float" de C# (y con el
--     Convert.ToSingle que usa GameRepository).
--   - Los DROP TABLE del principio hacen que el script sea re-ejecutable:
--     borra lo viejo antes de crear, asi podes correrlo varias veces sin
--     que explote por "la tabla ya existe".
-- =============================================================================


-- -----------------------------------------------------------------------------
-- 1) Limpieza (opcional, por si ya corriste el script antes)
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS Game;
DROP TABLE IF EXISTS "User";


-- -----------------------------------------------------------------------------
-- 2) Creacion de tablas
-- -----------------------------------------------------------------------------

CREATE TABLE "User" (
    Id              SERIAL PRIMARY KEY,
    UserName        VARCHAR(100)  NOT NULL,
    Email           VARCHAR(200)  NOT NULL,
    PasswordHash    VARCHAR(255)  NOT NULL,
    ProfilePicture  VARCHAR(500)  NOT NULL DEFAULT '',
    Description     TEXT          NOT NULL DEFAULT '',
    Followers       INT           NOT NULL DEFAULT 0,
    Followed        INT           NOT NULL DEFAULT 0,
    GamesOwned      INT           NOT NULL DEFAULT 0,
    Verified        INT           NOT NULL DEFAULT 0,   -- 0 = false, 1 = true
    VerifyHash      VARCHAR(255)  NOT NULL DEFAULT ''
);

CREATE TABLE Game (
    Id                    SERIAL PRIMARY KEY,
    IdPublisher           INT           NOT NULL REFERENCES "User"(Id),
    Date                  TIMESTAMP     NOT NULL,
    GameName              VARCHAR(200)  NOT NULL,
    Description           TEXT          NOT NULL DEFAULT '',
    State                 VARCHAR(20)   NOT NULL DEFAULT 'Private',
    NumberOfAchievements  INT           NOT NULL DEFAULT 0,
    PriceUSD              REAL          NOT NULL DEFAULT 0,
    DiscountPercentage    REAL          NOT NULL DEFAULT 0
);


-- -----------------------------------------------------------------------------
-- 3) Datos de ejemplo (seed)
--
-- Primero los usuarios, porque Game.IdPublisher apunta a User.Id. Si
-- insertamos un Game con IdPublisher = 1 y todavia no existe el User 1, la
-- base rechaza el INSERT por la foreign key.
-- -----------------------------------------------------------------------------

INSERT INTO "User"
    (UserName, Email, PasswordHash, ProfilePicture, Description,
     Followers, Followed, GamesOwned, Verified, VerifyHash)
VALUES
    ('pablo',       'pablo@indeura.com',     'hash_pablo',  '', 'Publisher docente',        10,  2, 3, 1, ''),
    ('alumno1',     'alumno1@indeura.com',   'hash_a1',     '', 'Alumno jugador',            0,  5, 1, 1, ''),
    ('indiestudio', 'contacto@indie.com',    'hash_indie',  '', 'Estudio indie de 3 personas', 50, 0, 2, 1, ''),
    ('sinverif',    'pendiente@indeura.com', 'hash_sv',     '', 'Cuenta recien creada',      0,  0, 0, 0, 'verify-abc-123');


INSERT INTO Game
    (IdPublisher, Date, GameName, Description, State,
     NumberOfAchievements, PriceUSD, DiscountPercentage)
VALUES
    (1, '2025-01-15 10:00:00', 'Aventura del Algoritmo',
        'Juego educativo para aprender algoritmos resolviendo puzzles.', 'Public',
        25, 19.99,  0),

    (1, '2025-06-20 14:30:00', 'SQL Quest',
        'Resolve misterios escribiendo consultas SQL en un mundo medieval.', 'Public',
        15,  9.99, 15),

    (1, '2026-02-10 09:00:00', 'Debug Masters',
        'Proximo lanzamiento: todavia en desarrollo, no visible al publico.', 'Private',
        0,  0,      0),

    (3, '2024-11-05 16:45:00', 'Pixel Dreams',
        'Plataformero indie en pixel art con banda sonora original.', 'Public',
        40, 14.99, 30),

    (3, '2025-08-01 11:20:00', 'Neon Racer',
        'Carreras ciberpunk a toda velocidad por ciudades lluviosas.', 'Public',
        30, 24.99,  0);


-- -----------------------------------------------------------------------------
-- 4) Chequeos rapidos (opcionales)
-- -----------------------------------------------------------------------------
-- SELECT Id, UserName, Email, Verified FROM "User";
-- SELECT Id, GameName, State, PriceUSD FROM Game;
-- SELECT COUNT(*) FROM Game WHERE State <> 'Private';  -- deberia dar 4
