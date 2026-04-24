-- Migration: add role column to chambre and ensure statut uses expected values
-- Run this script in your MySQL client or via your migration tool.
-- Backup your database before running.

ALTER TABLE chambre
  ADD COLUMN IF NOT EXISTS role VARCHAR(64) DEFAULT 'Général';

-- If your MySQL version doesn't support IF NOT EXISTS for ADD COLUMN, run this instead:
-- ALTER TABLE chambre ADD COLUMN role VARCHAR(64) DEFAULT 'Général';

-- Ensure statut column exists and has only 'Libre' or 'Occupée' values for display
-- If statut is nullable, you can set default to 'Libre'
ALTER TABLE chambre
  MODIFY COLUMN statut VARCHAR(32) NOT NULL DEFAULT 'Libre';

-- Optionally normalize existing statut values (lower/upper variations)
UPDATE chambre SET statut = 'Libre' WHERE statut IS NULL OR LOWER(statut) IN ('libre', 'libre ', ' free');
UPDATE chambre SET statut = 'Occupée' WHERE LOWER(statut) IN ('occupee', 'occupée', 'occupé', 'occupé ');

-- Create an index on statut if you filter by it frequently
CREATE INDEX IF NOT EXISTS idx_chambre_statut ON chambre(statut);

-- End of migration
