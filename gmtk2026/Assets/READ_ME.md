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

### Dialogues
Lp.    Znaki    Przykład użycia    Nazwa
1    |    |    Punkt przerwy tekstu
2    |~    |~    Punkt kończący tekst interpretowany
3    [id_skoku]    [5]    Punkt skoku
4    [[id_skoku]]    [[20]]    Punkt zaczepienia
5    #instrukcja#    #Money += 25#    Instrukcja zmiennej
6    @instrukcja@    @DISPLAY Money@    Instrukcja specjalna
7    &warunek&    &Money > 20&    Początek zakresu warunkowego
8    ||    ||    Punkt kończący zakres warunkowy
9    $[id_skoku] warunek$    $[10] Money >= 40$    Warunek ciągły
10    {
{tekst_opcji_1} {tekst_opcji_2}
}    {
{Powiedz: "Witaj."}
{Poczekaj.}
{Odejdź.}
}    Opcja / Zakres opcji
11    // tekst //    // Ukryty tekst //    Komentarz jednoliniowy
12    /* tekst */    /* Ukryty tekst */    Komentarz wieloliniowy

### Variables
Are stored in Dial text.
We have three types of variables:
- events
- stats
- items