

ALTER TABLE chambre
  ADD COLUMN IF NOT EXISTS role VARCHAR(64) DEFAULT 'Général';


ALTER TABLE chambre
  MODIFY COLUMN statut VARCHAR(32) NOT NULL DEFAULT 'Libre';

-- Optionally normalize existing statut values (lower/upper variations)
UPDATE chambre SET statut = 'Libre' WHERE statut IS NULL OR LOWER(statut) IN ('libre', 'libre ', ' free');
UPDATE chambre SET statut = 'Occupée' WHERE LOWER(statut) IN ('occupee', 'occupée', 'occupé', 'occupé ');


CREATE INDEX IF NOT EXISTS idx_chambre_statut ON chambre(statut);


