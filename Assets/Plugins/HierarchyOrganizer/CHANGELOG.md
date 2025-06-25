# 1.2.0 (2025-06-25)

### Features
- Extended "Create Folder Parent" functionality to allow selecting multiple siblings and group them under a new folder. Previously it only supported one at a time
- Converting a game object to a folder now resets its transform, moving its children to match their previous transform before the conversion 

### Fixes
- Fixed being able to convert game objects to folders in prefab stages
- Fixed being able to "Remove as Folder" on folders that have normal children with a lower sibling index than childed folders 
- Fixed folder conversion sometimes not converting all selected when converting multiple in the same hierarchy (e.g. both a parent and its child)
- Fixed tool visibility not updating when converting to folders and back to game objects

# 1.1.0 (2025-04-05)

### Features
- Added a "Get Started" section to the package README
- Added 3 options for what to happen when a disabled folder is stripped.
  - Destroy Children (Default)
  - Disable Children
  - Do Nothing (Previous behaviour)

### Fixes
- Fix certain settings not being saved correctly.  
Affected settings were the warning on non-stripped builds and all indent guide settings. 
