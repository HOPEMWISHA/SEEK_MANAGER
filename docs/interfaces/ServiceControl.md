Nom: ServiceControl
Fichier: SEEK_MANAGER\ServiceControl.cs
Description:
- Contrôle pour afficher les services (départements/sections) de l'hôpital.
- Probablement lié à l'entité `SERVICES` et au formulaire `ServiceDetailControl`.

Usage:
- Ajouter à un conteneur pour lister et sélectionner des services.
- Utiliser événements publics pour ouvrir `ServiceDetailControl`.

Dépendances:
- `SERVICES.cs`, `ServiceDetailControl.cs`, `Repository.cs`.

Remarques:
- Vérifier la disponibilité de méthodes `LoadServices()` et `SelectService(int id)`.
