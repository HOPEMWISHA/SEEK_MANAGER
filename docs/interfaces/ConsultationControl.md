Nom: ConsultationControl
Fichier: SEEK_MANAGER\ConsultationControl.cs
Description:
- Contrôle utilisateur pour l'affichage et la gestion des consultations médicales.
- Présente les éléments de `CONSULTATION` et permet de créer/éditer des consultations via `CONSULTATION-CS.cs` ou `CONSULTATION.cs`.

Usage:
- Placer `ConsultationControl` sur un formulaire principal ou une fenêtre dédiée.
- S'abonner aux événements exposés pour gérer la sélection/édition.

Dépendances:
- `CONSULTATION.cs`, `CONSULTATION-CS.cs`, `Repository.cs`.

Remarques:
- Vérifier l'existence de méthodes publiques `LoadData`, `Refresh`, ou `OpenConsultationForm`.
