## Future.UX.MVVM

Lightweight MVVM library for WPF with automatic INotifyPropertyChanged and command support. Works seamlessly with Future.UX.SourceGenerators for automatic property generation, including computed properties. Easy to install and start coding view models with minimal boilerplate.

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

Even with just this, the generated code will handle INotifyPropertyChanged for the fields and update FullName automatically when FirstName or LastName changes.

No need for additional usings or attributes. Just use the __ prefix and the generator does the rest.