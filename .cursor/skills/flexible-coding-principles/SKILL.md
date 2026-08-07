---
name: flexible-coding-principles
description: Guidelines for writing flexible, decoupled, and reusable code in AI and game development. Use when designing application architecture, implementing AI behaviors, or refactoring systems for better separation of concerns.
---

# Flexible Coding Principles

## Overview
This skill provides foundational principles and actionable techniques for creating flexible, decoupled, and reusable code. These concepts are essential for developing sophisticated AI skills and scalable game systems.

## Core Principles
1. **Respect Application Hierarchy**: Dependencies flow down, never up or laterally.
2. **Data and Logic Cohesion**: Keep data and the logic that operates on it together (Encapsulation).
3. **Component-Based Architecture**: Use composition over inheritance to compartmentalize functionality.
4. **Dependency Injection**: Supply references at runtime for flexibility and DRY code.
5. **Essential Design Patterns**: Utilize State Machines, Observer, and Mediator patterns.
6. **Iterative Approach**: Prioritize practice and shipping over perfect theory.

## Detailed Guidance
For in-depth explanations and implementation details of each principle, see [principles.md](references/principles.md).

## Usage Scenarios
- **Designing Systems**: When planning a new game system or AI behavior, use these principles to ensure modularity.
- **Refactoring**: Use as a checklist when decoupling tightly bound objects.
- **Code Review**: Apply these principles to identify architectural weaknesses in existing code.
