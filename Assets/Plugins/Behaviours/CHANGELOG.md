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
