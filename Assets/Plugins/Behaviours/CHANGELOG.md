# 0.5.0 (2025-08-12)

### Features
- Added a minimap panel in the bottom right corner that shows nodes, connection and groups

### Fixes
- Fixed connections sometimes causing null refs without an active event
- Fixed framing action (Shortcut 'F') centering with a slight offset

# 0.4.0 (2025-06-03)

### Features
- Created `DoUntil` composite which executes its first child until another child either returns running or success
- Created `WaitForEvent` decorator. Works the same as `WaitUntil` with a `CheckEvent` condition but is more performant if only checking for event
- Added new `ResetOnTickFinish` policy to `Cooldown` decorators
- Slightly reduced click area on connections

### Fixes
- Fixed error when exiting playmode with the editor window as a hidden tab
- Fixed editor window connections intercepting events in a larger area than intended

# 0.3.1 (2025-05-14)

### Features
- Now groups multiple instances of a missed type into one entry in the validation panel
- Added so the validation panel now highlights if manually editing a missing type to an existing type
- Added per owner cache of targets used by actions/conditions/operations

### Fixes
- Fixed missing type entries not remembering previous changes if hidden by the scroll view
- Fixed missing type repair button not working when any list entry was hidden by the scroll view
- Fix validation panel search not adding declared types in the class name

# 0.3.0 (2025-05-12)

### Features
- Now remembers the root owner when navigating nested behaviours
- Swapped branch composite directions i.e. left is false and right is true
- Added dynamic option on branch composite to reevaluate each tick
- Added copy-paste support on behaviour nodes via the clipboard
- Add utility method to initialize and then start a `BehaviourOwner`. Meant to be used together with the manual start mode.
- Changed how `BlackboardRef<>` and `BlackboardOnlyRef<>` track their assigned behaviour. Now using reflection instead of property drawer
- Added int to float and float to int blackboard conversions
- Created an API on `BlackboardConverter` to add custom conversions in runtime
- Updated behaviour validation panel layout with more info and search functionality

### Fixes
- Added auto save on all changed behaviours before script reloads. This fixes an issue where repair sometimes seemingly does not work even though you change to a valid type

# 0.2.0 (2025-04-28)

### Features
- Replaced previous implementation of code generation with an incremental source generator (thanks [@smeas](https://github.com/smeas))
- Split `BehaviourAction.OnTick()` responsibility into `OnEnter()`, `OnTick()` and `OnChildFinished()`
- Exposed `BehaviourOwner` API and added additional guard statements to handle different settings
- Added support for abstract types in blackboards and connecting blackboard references to non-exact blackboard variables with an assignable type
- Added and fixed undo support for most actions within a graph
  - Nodes
  - Connections
  - Groups
  - Managed lists (Conditions/Operations)
  - Blackboard variables (Excl. runtime values)
  - Blackboard references
- Replaced `BlackboardOnly` attribute with `BlackboardOnlyRef<>` type
- Added ability to change a blackboard variable's type
  - This also serves the purpose of being able to repair blackboard variables if their type is lost instead of having to recreate them
- Made blackboard variable value fields delayed (if applicable)
- Added autofocus on the name of new blackboard variables
- Disabled AutoReferenced on the editor assembly

### Fixes
- Fixed being able to edit connections of a runtime behaviour instance
- Fixed smart delete not reconnecting children to parents with unlimited child capacity
- Fixed certain types (e.g. structs excl. `Vector2`, `Vector3`, `Vector4`) not being serialized correctly in the blackboard
- Fixed managed lists not updating inspection when removing items
- Fixed blackboard variables losing their guid if created with a redo
- Fixed blackboard drawer not updating correctly with multiple drawers active at once
- Fixed certain blackboard variable styling being different in the inspector compared to the editor window 

# 0.1.1 (2025-04-06)

### Fixes
- Removed UNITY_EDITOR preprocessor directive on BlackboardRef and BehaviourConditionGroup `Editor_Info` properties to not hinder builds in case of overrides not using the same directives.
