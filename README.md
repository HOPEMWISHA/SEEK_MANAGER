voici le script de ma base de données

CREATE DATABASE `HopitalDB`
  DEFAULT CHARACTER SET utf8mb4
  DEFAULT COLLATE utf8mb4_general_ci;
USE `HopitalDB`;
CREATE TABLE `users` (
  `id_user` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  `username` VARCHAR(100) NOT NULL UNIQUE,
  `password_hash` VARCHAR(255) NOT NULL,
  `full_name` VARCHAR(150) NOT NULL,
  `email` VARCHAR(150) NOT NULL UNIQUE,
  `telephone` VARCHAR(50) DEFAULT NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `service` (
  `id_service` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  `nom_service` VARCHAR(200) NOT NULL,
  `description` TEXT NULL,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
CREATE TABLE `patient` (
  `id_patient` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  `nom` VARCHAR(200) NOT NULL,
  `prenom` VARCHAR(200) NULL,
  `sexe` ENUM('M','F','O') DEFAULT 'O',
  `date_naissance` DATE NULL,
  `telephone` VARCHAR(50) NULL,
  `adresse` VARCHAR(500) NULL,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
CREATE TABLE `medecin` (
  `id_medecin` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  `nom` VARCHAR(200) NOT NULL,
  `specialite` VARCHAR(200) NULL,
  `id_service` INT NULL,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT `fk_medecin_service` FOREIGN KEY (`id_service`) REFERENCES `service` (`id_service`)
    ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
CREATE TABLE `hospitalisation` (
  `id_hospitalisation` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  `chambre` VARCHAR(50) NULL,
  `id_patient` INT NOT NULL,
  `id_service` INT NOT NULL,
  `id_medecin` INT NULL,
  `date_entree` DATE NOT NULL,
  `date_sortie` DATE NULL,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT `fk_hosp_patient` FOREIGN KEY (`id_patient`) REFERENCES `patient` (`id_patient`)
    ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_hosp_service` FOREIGN KEY (`id_service`) REFERENCES `service` (`id_service`)
    ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `fk_hosp_medecin` FOREIGN KEY (`id_medecin`) REFERENCES `medecin` (`id_medecin`)
    ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
CREATE TABLE `consultation` (
  `id_consultation` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  `date_consultation` DATETIME NOT NULL,
  `diagnostic` TEXT NULL,
  `traitement` TEXT NULL,
  `id_patient` INT NOT NULL,
  `id_medecin` INT NOT NULL,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT `fk_consult_patient` FOREIGN KEY (`id_patient`) REFERENCES `patient` (`id_patient`)
    ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `fk_consult_medecin` FOREIGN KEY (`id_medecin`) REFERENCES `medecin` (`id_medecin`)
    ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE INDEX `idx_patient_nom` ON `patient` (`nom`);
CREATE INDEX `idx_medecin_service` ON `medecin` (`id_service`);
CREATE INDEX `idx_hosp_service` ON `hospitalisation` (`id_service`);
CREATE INDEX `idx_consult_date` ON `consultation` (`date_consultation`);
CREATE INDEX idx_users_username ON `users` (`username`);
CREATE INDEX idx_users_email ON `users` (`email`);

ALTER TABLE `patient`
ADD COLUMN `age` INT; 

DELIMITER $$
CREATE TRIGGER `patient_before_insert`
BEFORE INSERT ON `patient`
FOR EACH ROW
BEGIN 
  SET NEW.age = TIMESTAMPDIFF(YEAR, NEW.`date_naissance`, CURDATE());
END$$

CREATE TRIGGER `patient_before_update`
BEFORE UPDATE ON `patient`
FOR EACH ROW
BEGIN
  SET NEW.age = TIMESTAMPDIFF(YEAR, NEW.`date_naissance`, CURDATE());
END$$
DELIMITER ;


voici notre modelisation 


<img width="1536" height="1024" alt="ChatGPT Image 21 avr  2026, 22_45_52" src="https://github.com/user-attachments/assets/5375d54f-959d-4aa4-bcc4-5f2cf1055a84" />
