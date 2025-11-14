![Release](https://img.shields.io/badge/release-v1.0.0.2-blue)
![NuGet](https://img.shields.io/nuget/v/Future.UX.SourceGenerators)
![NuGet](https://img.shields.io/nuget/v/Future.UX.MVVM)

# Future.UX

**Experimental MVVM toolkit + C#14 source generator** for ultra-lightweight, boilerplate-free MVVM in WPF.

- `Future.UX.SourceGenerators` – Incremental source generator: converts `__`-prefixed fields and methods into **bindable properties** and **commands** automatically.
- `Future.UX.MVVM` – Runtime MVVM helpers (`RelayCommand` / `AsyncRelayCommand`) to handle sync and async commands cleanly.
- `Future.UX.Test` – Playground & sample projects to try things out.


## Quick Start

1. Clone the repository:
   ```bash
   git clone <your-repo-url>

2. Open the solution in Visual Studio 2026.

3. Build & run Future.UX.Test to see examples in action.


## Features

Auto-generates properties and INotifyPropertyChanged wiring from __-prefixed fields.

Auto-generates concrete RelayCommand and AsyncRelayCommand properties from private __ methods.

Supports CanExecute methods for commands.

Minimal boilerplate – just define your fields & methods, the generator does the rest.

Fully compatible with XAML bindings and CommandParameter.


## Installation

Install the MVVM package:

      Install-Package Future.UX.MVVM

For full automatic property generation, also include the source generator:

      Install-Package Future.UX.SourceGenerators


## Example

      public partial class Person
      {
          private string? __firstName;
          private string? __lastName;
          // Computed property
          public string FullName => $"{FirstName} {LastName}";
      }

Even with just this, the generator will create the FirstName and LastName properties with INotifyPropertyChanged and also update FullName automatically when FirstName or LastName changes.

No need for additional usings or attributes. Just use the __ prefix and the generator does the rest.


## Contributing

Open a PR against the dev branch.

Keep main protected; we like it stable. 😎


## License

MIT – see the LICENSE file.
