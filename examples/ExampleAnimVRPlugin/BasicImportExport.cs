using AnimVRFilePlugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

[CustomImporter(Extension = "basic")]
public class BasicImportExport : CustomImporter {

    public override List<PlayableData> Import(string path) {
        return new List<PlayableData> { new SymbolData() { displayName = Path.GetFileNameWithoutExtension(path) } };
    }

    public override void Export(StageData stage, string path) {
        File.WriteAllText(path, "Selected Playable:" + stage.activePlayablePath);
    }
}
