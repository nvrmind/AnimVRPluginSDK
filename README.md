# AnimVR Plugin SDK
Write custom importers and exporters for AnimVR in C#

## Development
Checkout out the project in /examples to see how to set up Visual Studio for plugin development.

## API
```CSharp
// Inherit from this class
public class CustomImporter
{
    // Override this function to indicate import support.
    public virtual List<PlayableData> Import(string path) { }
    // Override this function to indicate export support.
    public virtual void Export(StageData stage, string path)  { }
}

// Mark your class with this attribute to tell AnimVR what file extension you want to support.
public class CustomImporterAttribute : Attribute
{
    public string Extension;
}

// Example
[CustomImporter(Extension = "example")]
public class ExamplePlugin : CustomImporter
{
    public override List<PlayableData> Import(string path)
    {
        SymbolData result = new SymbolData();
        result.displayName = Path.GetFileNameWithoutExtension(path);
        return new List<PlayableData>() { result };
    }
    
    public override void Export(StageData stage, string path)
    {
        File.WriteAllText(path, stage.name);
    }
}

```

## Distribution
Place the resulting .dll file into Documents/AnimVR/Plugins and AnimVR will load your plugin the next time you start the program.
