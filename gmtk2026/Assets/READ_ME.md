# Project Rules & Guidelines

## Naming Conventions
- Assets use camelCase (e.g., dialogueClick.mp3).
- Scripts use PascalCase (e.g., Gameplay.cs).

## Scripting Rules
- Code and comments must be in English.
- Follow the Single Responsibility Principle (SRP).
- Decouple managers using Singletons or C# Events.
- names for Audio, Audio variables in Gameplay and AudioManager should be the same !!!

## Core Architecture
- Gameplay: Core State Machine handling game flow. No UI or Audio logic.
- Dial: Dialogue engine for parsing text files and tags.
- AudioManager: Singleton handling all BGM and SFX.
- UIManager: Presentation layer for updating text and spawning visuals.
- Menu: Main Menu logic and initial scene transitions.

## Narrative & Tone
- Story driven by a narrator (first-person internal monologue).
- Style inspired by Bareja movies.

## Mechanics

### Variables
Are stored in Dial text.
We have three types of variables:
- events
- stats
- items