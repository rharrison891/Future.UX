# Future.UX

**Experimental MVVM toolkit + C#14 source generator** for ultra-lightweight, boilerplate-free MVVM in WPF.

- `Future.UX.SourceGenerators` – Incremental source generator: converts `__`-prefixed fields and methods into **bindable properties** and **commands** automatically.
- `Future.UX.MVVM` – Runtime MVVM helpers (`RelayCommand` / `AsyncRelayCommand`) to handle sync and async commands cleanly.
- `Future.UX.Test` – Playground & sample projects to try things out.

## Quick Start

1. Clone the repository:
   ```bash
   git clone Future.UX.SourceGenerators

2. Open the solution in Visual Studio 2026.


3. Build & run Future.UX.Test to see examples in action.



Features

Auto-generates properties and INotifyPropertyChanged wiring from __-prefixed fields.

Auto-generates concrete RelayCommand and AsyncRelayCommand properties from private __ methods.

Supports CanExecute methods for commands.

Minimal boilerplate – just define your fields & methods, the generator does the rest.

Fully compatible with XAML bindings and CommandParameter.

**NEW**
Theme generator: auto-generate application wide resources for colors, brushes, and styles from a single Dictionary.
Brush Extension allows inline tweaks to existing brushes (e.g., change opacity, or brightness).
## Example
```csharp	
    private static readonly Dictionary<string, Color> __baseColors = new() {
        { "Background",Color.FromArgb(255,30,30,30)  },
        { "Foreground", Color.FromArgb(255,220,220,220) },
        { "Primary", Color.FromArgb(255,0,120,215) },
        { "Secondary", Color.FromArgb(255,32,32,32) },
        { "Accent", Color.FromArgb(255,0,153,204) },
        { "Border", Color.FromArgb(255,100,100,100) },
        { "Error", Color.FromArgb(255,232,17,35) },
        { "Warning", Color.FromArgb(255,255,185,0) },
        { "Success", Color.FromArgb(255,16,124,16) }
    };

    private static void Generated() { 
        var brush = Theme.GetBrush(ThemeColor.Primary);
        var color= Theme.GetColor(ThemeColor.Accent);
        var brushExtension=new BrushBaseExtension(baseColor: ThemeColor.Background, alpha: 0.5, brightness:-20);
        var modBrush= brushExtension.ProvideValue(null);
        var colorExtension = new BrushBaseExtension(baseColor: ThemeColor.Error, alpha: 0.8, brightness: 30, asColor: true);
        var modColor = colorExtension.ProvideValue(null);
    } 
```
```xaml
    <TextBlock HorizontalAlignment="Center"
               VerticalAlignment="Center"
               FontSize="30"
               Foreground="{t:BrushBase Base=Accent, Alpha=.5, Brightness=0}"
               Text="&#xE700;"
               Padding="5"
               FontFamily="{StaticResource SegoeFluent}"
               MouseLeftButtonDown="TextBlock_MouseLeftButtonDown">
        <TextBlock.Background>
            <LinearGradientBrush StartPoint="1,1"
                                 EndPoint="0,0">
                <GradientStop Offset="0"
                              Color="{t:BrushBase AsColor=True, Base=Background, Alpha=1, Brightness=10}" />
                <GradientStop Offset="1"
                              Color="{t:BrushBase AsColor=True, Base=Background, Alpha=0.75, Brightness=10}" />
            </LinearGradientBrush>
        </TextBlock.Background>
        <TextBlock.Triggers>
            <EventTrigger RoutedEvent="MouseEnter">
                <BeginStoryboard>
                    <Storyboard>
                        <ColorAnimation Storyboard.TargetProperty="Foreground.Color"
                                        To="{t:BrushBase AsColor=True, Base=Accent, Alpha=1, Brightness=50}"
                                        Duration="0:0:0.3" />
                    </Storyboard>
                </BeginStoryboard>
            </EventTrigger>
            <EventTrigger RoutedEvent="MouseLeave">
                <BeginStoryboard>
                    <Storyboard>
                        <ColorAnimation Storyboard.TargetProperty="Foreground.Color"
                                        To="{t:BrushBase AsColor=True, Base=Accent, Alpha=1, Brightness=0}"
                                        Duration="0:0:0.3" />
                    </Storyboard>
                </BeginStoryboard>
            </EventTrigger>
        </TextBlock.Triggers>
    </TextBlock>
```

# Font Generator

Auto generates FontFamily resources with enums and FontFamilyExtension for easy use in XAML.

Add a Fonts folder to your project and add font files there. The generator will pick them up automatically.

Example structure:

YourProject

```
├── Fonts   
│   ├── SegoeFluent
│   │   ├── SegoeFluent-Regular.ttf
│   │   ├── SegoeFluent-Semibold.ttf
│   │   └── SegoeFluent-Bold.ttf
│   └── AnotherFont

```

```xaml

<TextBlock FontFamily="{t:FontFamily Font=SegoeFluent, Weight=Semibold}" 
           FontSize="24" 
           Text="Hello, Future.UX!" />
```

Contributing

Open a PR against the dev branch.

Keep main protected; we like it stable. 😎


License

MIT – see the LICENSE file.
