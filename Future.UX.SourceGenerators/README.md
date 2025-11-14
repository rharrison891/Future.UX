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



Features

Auto-generates properties and INotifyPropertyChanged wiring from __-prefixed fields.

Auto-generates concrete RelayCommand and AsyncRelayCommand properties from private __ methods.

Supports CanExecute methods for commands.

Minimal boilerplate – just define your fields & methods, the generator does the rest.

Fully compatible with XAML bindings and CommandParameter.


Contributing

Open a PR against the dev branch.

Keep main protected; we like it stable. 😎


License

MIT – see the LICENSE file.

This one is complete and should copy all the way down. 