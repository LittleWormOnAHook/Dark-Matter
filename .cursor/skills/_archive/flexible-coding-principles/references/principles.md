# Flexible Coding Principles - Detailed Guidance

This document provides actionable techniques for creating flexible, decoupled, and reusable code.

## 1. Respecting the Application Hierarchy
Dependencies should only flow **down** the hierarchy, never up or laterally.

* **Decoupling:** Aim to go down only one level for any dependency. This ensures objects can work in isolation.
* **Isolating Objects:** Avoid assumptions about "sibling" objects. A player character should not depend on a specific "Items Manager" sibling; instead, it should be passed what it needs or use a common interface.
* **Separation of Concerns:** High-level objects should request actions from low-level objects rather than manipulating their internal data. (e.g., "Tell" don't "Ask").

## 2. Data and Logic Cohesion
Keep data and the logic that operates on that data together.

* **Encapsulation:** Errors are contained within the relevant object. If animation logic is inside the animation container, animation bugs stay there.
* **Interface Consistency:** Different types (Enemies, NPCs, Players) can all use the same interface to manage shared behaviors.

## 3. Component-Based Architecture
Use **components** to compartmentalize functionality instead of large monolithic objects or deep inheritance.

* **Composition over Inheritance:** Create reusable components like "Health" or "Stats" that can be attached to any entity.
* **Efficiency:** Removes "dead code" and "God Object" patterns.

## 4. Dependency Injection
Supply references to objects at **runtime** rather than hard-coding them at compile time.

* **Runtime Flexibility:** Logic can operate on generic types, with specific instances passed in at runtime.
* **DRY Principle:** Allows the same logic to operate on different objects without duplication.

## 5. Essential Design Patterns for AI
1. **State Machines:** Vital for managing complex behaviors and transitions.
2. **Observer Pattern:** Allows objects to "listen" for events without tight coupling to the sender.
3. **Mediator Pattern:** Coordinated communication between multiple objects when simple observation is insufficient.

## 6. The Iterative Approach
Prioritize practice over perfect theory. Ship functional code, hit pain points, and then refactor using these patterns to solve specific problems.
