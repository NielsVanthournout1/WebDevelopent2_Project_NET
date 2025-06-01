-- --------------------------------------------------
--  Seed Data for SID-in-Beurzen Administrative System
-- --------------------------------------------------

START TRANSACTION;

-- 1) Roles
INSERT INTO `RoleType` (`RoleName`) VALUES
  ('Exposant'),
  ('MarketingStaff'),
  ('MarketingLead'),
  ('Administrator');

-- 2) Accounts (Users)
INSERT INTO `Accounts`
  (`FirstName`,`LastName`,`EmailAddress`,`PasswordHash`,`RoleTypeId`,`IsActive`,`RegistrationToken`)
VALUES
  -- Exposants (mobile page users)
  ('Tom','Verbeek','tom.verbeek@expo.local','HASHED_pw1', 1, 1, UUID()),
  ('Anke','deVries','anke.devries@expo.local','HASHED_pw2', 1, 1, UUID()),

  -- Marketing staff
  ('Sofie','Jansen','sofie.jansen@marketing.local','HASHED_pw3', 2, 1, UUID()),
  ('Wim','Bos','wim.bos@marketing.local','HASHED_pw4', 2, 1, UUID()),

  -- Marketing team lead
  ('Emma','vanDijk','emma.vandijk@marketing.local','HASHED_pw5', 3, 1, UUID()),

  -- Administrators
  ('Admin','User','admin@sidin.local','HASHED_pw6', 4, 1, UUID());

-- 3) Study Programs
INSERT INTO `Program` (`ProgramName`) VALUES
  ('Computer Science'),
  ('Information Technology'),
  ('Electrical Engineering'),
  ('Artificial Intelligence'),
  ('Multimedia Arts'),
  ('Cybersecurity');

-- 4) Trade Shows
INSERT INTO `TradeShow` (`Title`,`EventDate`,`Venue`) VALUES
  ('SID Fair 2023','2023-06-15','Amsterdam RAI'),
  ('SID Fair 2024','2024-06-20','Brussels Expo'),
  ('SID Fair 2025','2025-06-25','Rotterdam Ahoy');

-- 5) Candidates (Studiekiezers)
INSERT INTO `Candidate`
  (`GivenName`,`Surname`,`BirthDate`,`Institution`,`FieldOfStudy`,`QRCodeLink`)
VALUES
  ('Lisa','Meijer','2000-02-10','Hogeschool Utrecht','Computer Science','https://sidin.local/qr/1001'),
  ('Mark','Smit','1999-11-23','TU Delft','Electrical Engineering','https://sidin.local/qr/1002'),
  ('Nina','Kraan','2001-05-05','Hogeschool Rotterdam','Multimedia Arts','https://sidin.local/qr/1003'),
  ('Joris','Vos','2002-07-17','Fontys','Information Technology','https://sidin.local/qr/1004'),
  ('Sara','deBoer','2000-09-30','Eindhoven University','Artificial Intelligence','https://sidin.local/qr/1005'),
  ('Mohammed','Ali','2001-12-12','Avans','Cybersecurity','https://sidin.local/qr/1006');

-- 6) Registrations
--    A few candidates visit multiple fairs on different dates 
INSERT INTO `Registration`
  (`CandidateId`,`TradeShowId`,`Notes`,`RegisteredAt`)
VALUES
  -- Lisa attends 2023, 2024
  (1, 1, 'Wants CS & AI info',      '2023-06-15 10:05:00'),
  (1, 2, 'Follow-up visit',         '2024-06-20 11:20:00'),

  -- Mark attends 2024 only
  (2, 2, 'Interested in EE lab tour','2024-06-20 09:45:00'),

  -- Nina attends 2023 & 2025
  (3, 1, 'Portfolio review',        '2023-06-15 12:30:00'),
  (3, 3, 'Wants multimedia demo',   '2025-06-25 14:10:00'),

  -- Joris attends 2025
  (4, 3, 'IT security track',       '2025-06-25 10:50:00'),

  -- Sara attends all three fairs
  (5, 1, 'AI ethics talk',          '2023-06-15 11:00:00'),
  (5, 2, 'Machine learning workshop','2024-06-20 13:15:00'),
  (5, 3, 'Research collaboration',  '2025-06-25 15:00:00'),

  -- Mohammed attends 2024 & 2025
  (6, 2, 'Cybersecurity challenge', '2024-06-20 16:30:00'),
  (6, 3, 'Network security panel',  '2025-06-25 09:00:00');

-- 7) Link each registration to one or more programs
INSERT INTO `RegistrationProgram` (`RegistrationId`,`ProgramId`) VALUES
  -- Lisa @ 2023: CS + AI
  (1, 1),(1, 4),
  -- Lisa @ 2024: CS only
  (2, 1),

  -- Mark @ 2024: EE + Cybersecurity
  (3, 3),(3, 6),

  -- Nina @ 2023: Multimedia only
  (4, 5),
  -- Nina @ 2025: Multimedia + IT
  (5, 5),(5, 2),

  -- Joris @ 2025: IT + Cybersecurity
  (6, 2),(6, 6),

  -- Sara @ 2023: AI + CS
  (7, 4),(7, 1),
  -- Sara @ 2024: AI only
  (8, 4),
  -- Sara @ 2025: AI + EE + CS
  (9, 4),(9, 3),(9, 1),

  -- Mohammed @ 2024: Cybersecurity only
  (10,6),
  -- Mohammed @ 2025: Cybersecurity + IT
  (11,6),(11,2);

COMMIT;
