-- 1) Migration history
CREATE TABLE IF NOT EXISTS `MigrationRecords` (
    `MigrationKey` VARCHAR(150) NOT NULL,
    `VersionInfo` VARCHAR(32) NOT NULL,
    PRIMARY KEY (`MigrationKey`)
);

START TRANSACTION;

-- 2) Roles
CREATE TABLE `RoleType` (
    `RoleTypeId` INT NOT NULL AUTO_INCREMENT,
    `RoleName` VARCHAR(50) NOT NULL,
    PRIMARY KEY (`RoleTypeId`)
);

-- 3) Users
CREATE TABLE `Accounts` (
    `AccountId` INT NOT NULL AUTO_INCREMENT,
    `FirstName` VARCHAR(50) DEFAULT NULL,
    `LastName` VARCHAR(50) DEFAULT NULL,
    `EmailAddress` VARCHAR(100) DEFAULT NULL,
    `PasswordHash` VARCHAR(255) DEFAULT NULL,
    `RoleTypeId` INT DEFAULT NULL,
    `IsActive` TINYINT(1) DEFAULT NULL,
    `RegistrationToken` CHAR(36) DEFAULT NULL,
    PRIMARY KEY (`AccountId`),
    CONSTRAINT `FK_Accounts_RoleType`
      FOREIGN KEY (`RoleTypeId`) REFERENCES `RoleType` (`RoleTypeId`) ON DELETE RESTRICT
);

-- 4) Candidates
CREATE TABLE `Candidate` (
    `CandidateId` INT NOT NULL AUTO_INCREMENT,
    `GivenName` VARCHAR(50) DEFAULT NULL,
    `Surname` VARCHAR(50) DEFAULT NULL,
    `BirthDate` DATE DEFAULT NULL,
    `Institution` VARCHAR(100) DEFAULT NULL,
    `FieldOfStudy` VARCHAR(100) DEFAULT NULL,
    `QRCodeLink` VARCHAR(255) DEFAULT NULL,
    PRIMARY KEY (`CandidateId`)
);

-- 5) Trade shows
CREATE TABLE `TradeShow` (
    `TradeShowId` INT NOT NULL AUTO_INCREMENT,
    `Title` VARCHAR(100) DEFAULT NULL,
    `EventDate` DATE DEFAULT NULL,
    `Venue` VARCHAR(100) DEFAULT NULL,
    PRIMARY KEY (`TradeShowId`)
);

-- 6) Programs
CREATE TABLE `Program` (
    `ProgramId` INT NOT NULL AUTO_INCREMENT,
    `ProgramName` VARCHAR(100) DEFAULT NULL,
    PRIMARY KEY (`ProgramId`)
);

-- 7) Registrations
CREATE TABLE `Registration` (
    `RegistrationId` INT NOT NULL AUTO_INCREMENT,
    `CandidateId` INT DEFAULT NULL,
    `TradeShowId` INT DEFAULT NULL,
    `Notes` TEXT DEFAULT NULL,
    `RegisteredAt` DATETIME DEFAULT NULL,
    PRIMARY KEY (`RegistrationId`),
    CONSTRAINT `FK_Registration_Candidate`
      FOREIGN KEY (`CandidateId`) REFERENCES `Candidate` (`CandidateId`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Registration_TradeShow`
      FOREIGN KEY (`TradeShowId`) REFERENCES `TradeShow` (`TradeShowId`) ON DELETE RESTRICT
);

-- 8) Registration ↔ Program link
CREATE TABLE `RegistrationProgram` (
    `RegistrationId` INT NOT NULL,
    `ProgramId` INT NOT NULL,
    PRIMARY KEY (`RegistrationId`, `ProgramId`),
    CONSTRAINT `FK_RegistrationProgram_Registration`
      FOREIGN KEY (`RegistrationId`) REFERENCES `Registration` (`RegistrationId`) ON DELETE CASCADE,
    CONSTRAINT `FK_RegistrationProgram_Program`
      FOREIGN KEY (`ProgramId`) REFERENCES `Program` (`ProgramId`) ON DELETE RESTRICT
);

-- 9) Indexes
CREATE INDEX `IDX_Accounts_RoleType` ON `Accounts` (`RoleTypeId`);
CREATE INDEX `IDX_Registration_Candidate` ON `Registration` (`CandidateId`);
CREATE INDEX `IDX_Registration_TradeShow` ON `Registration` (`TradeShowId`);
CREATE INDEX `IDX_RegistrationProgram_Program` ON `RegistrationProgram` (`ProgramId`);

-- 10) Seed migration history
INSERT INTO `MigrationRecords` (`MigrationKey`, `VersionInfo`)
VALUES ('20250527114910_ExportToSql', '8.0.13');

COMMIT;
