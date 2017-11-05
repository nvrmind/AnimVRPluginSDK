using AnimVRFilePlugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

[CustomImporter(Extension = "timeframe")]
public class ObjectSquence : CustomImporter
{
    public override List<PlayableData> Import(string path)
    {
        var lines = File.ReadAllLines(path);

        SymbolData result = new SymbolData();
        result.displayName = Path.GetFileNameWithoutExtension(path);

        string basePath = Path.GetDirectoryName(path) + "/";

        int currentFrame = 0;
        foreach (var line in lines)
        {
            var parts = line.Split(' ');
            float duration = float.Parse(parts[0]);
            string modelFile = basePath + parts[1];

            List<StaticMeshData> meshes;
            if (!AnimAssimpImporter.ImportFile(modelFile, false, out meshes)) continue;

            int frameDuration = Mathf.FloorToInt(duration * 12);

            foreach (StaticMeshData meshData in meshes)
            {
                meshData.AbsoluteTimeOffset = currentFrame;
                meshData.Timeline.Frames.Clear();
                meshData.InstanceMap.Clear();
                meshData.LoopIn = AnimVR.LoopType.OneShot;
                meshData.LoopOut = AnimVR.LoopType.OneShot;

                for (int i = 0; i < frameDuration; i++)
                {
                    var frame = new SerializableTransform();
                    meshData.Timeline.Frames.Add(frame);
                    meshData.InstanceMap.Add(frame);
                }

                result.Playables.Add(meshData);
            }

            currentFrame += frameDuration;
        }


        return new List<PlayableData>() { result };
    }
}
