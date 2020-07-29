# FiniteStateMachine
A Finite State Machine implementation in C#

Include:
* Suports generic type symbols
* Nondeterministic Finite State Machine
* Deterministic Finite State Machine
  * Build from a Nondeterministic Finite State Machine or created from scratch
  * Supports minimization
* Regular Expression (subset of the .Net version) to DFA builder
  * Supports the following operations
    *  Or operator (|)
    *  Grouping (())
    *  Kleene operator (* - zero or more)
    *  Plus operator (+ - one or more)
    *  Charset (with range and negation options) ([a] - a char, [^a] - all but a, [a-z] - from a to z)
    *  Any char match (.)

**Note**
States are represented by positive integers.
Start state is **always** state 0

## Compilation
This project was build using .Net Core SDK v2.1

Compilation steps:
* .Net Core SDK (v2) is required
* On a command line type the following command
  '''
  dotnet build --configuration Release
  '''
