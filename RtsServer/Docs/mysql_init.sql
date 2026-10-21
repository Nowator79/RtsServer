-- Инициализация MySQL для RtsServer.
-- Логин по умолчанию в ConfigGameServer: root/root, база rts_server.
--
--   mysql -uroot -proot < Docs/mysql_init.sql

CREATE DATABASE IF NOT EXISTS rts_server
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE rts_server;

CREATE TABLE IF NOT EXISTS Users (
  Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  UserName VARCHAR(64) NOT NULL,
  Password VARCHAR(128) NOT NULL,
  AuthToken VARCHAR(64) NULL,
  Credits INT NOT NULL DEFAULT 0,
  TechCredits INT NOT NULL DEFAULT 0,
  Experience INT NOT NULL DEFAULT 0,
  Level INT NOT NULL DEFAULT 1,
  UNIQUE KEY IX_Users_UserName (UserName),
  KEY IX_Users_AuthToken (AuthToken)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Если таблица уже была без токена:
-- ALTER TABLE Users ADD COLUMN AuthToken VARCHAR(64) NULL;
-- Старые аккаунты с Level=0 клиент/сервер показывают как 1.

INSERT INTO Users (UserName, Password, Credits, TechCredits, Experience, Level)
SELECT 'test', 'test', 0, 0, 0, 1
WHERE NOT EXISTS (SELECT 1 FROM Users WHERE UserName = 'test');
