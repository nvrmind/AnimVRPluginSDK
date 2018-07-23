using AnimVRFilePlugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using AnimVRData;
using FlatBuffers;

[CustomImporter(Extension = "flatanim")]
public class Flabuffers : CustomImporter
{

    #region Import
    public override List<PlayableData> Import(string path)
    {
        byte[] bytes = null;
        {
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            bytes = File.ReadAllBytes(path);
            stopwatch.Stop();
            Debug.Log("Loading bytes in " + stopwatch.ElapsedMilliseconds + "ms.");
        }

        SymbolData result = new SymbolData();

        {
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var buf = new ByteBuffer(bytes);
            var stage = Stage.GetRootAsStage(buf);

            result.displayName = Path.GetFileNameWithoutExtension(path);

            for (int i = 0; i < stage.SymbolsLength; i++)
            {
                var symbol = ReadSymbol(stage.Symbols(i));
                result.Playables.Add(symbol);
            }
            stopwatch.Stop();
            Debug.Log("Creating data in " + stopwatch.ElapsedMilliseconds + "ms.");
        }

        return new List<PlayableData>() { result };
    }

    SymbolData ReadSymbol(Symbol? symbol)
    {
        var result = new SymbolData();
        TransferPlayableData(symbol.Value.Base, ref result);

        for(int i = 0; i < symbol.Value.TimelinesLength; i++)
        {
            result.Playables.Add(ReadTimeline(symbol.Value.Timelines(i)));
        }

        for (int i = 0; i < symbol.Value.SymbolsLength; i++)
        {
            result.Playables.Add(ReadSymbol(symbol.Value.Symbols(i)));
        }

        return result;
    }

    void TransferPlayableData<T>(Playable? playable, ref T target) where T : PlayableData
    {
        target.displayName = playable.Value.DisplayName;
        target.transform = ReadTransform(playable.Value.Trans);
        target.opacity = playable.Value.Opacity;
    }

    TimeLineData ReadTimeline(Timeline? timeline)
    {
        var result = new TimeLineData();
        TransferPlayableData(timeline.Value.Base, ref result);

        for (int i = 0; i < timeline.Value.FramesLength; i++)
        {
            result.Frames.Add(ReadTimelineFrame(timeline.Value.Frames(i)));
        }

        return result;
    }

    SerializableTransform ReadTransform(AnimVRData.Transform? trans)
    {
        var result = new SerializableTransform();
        result.pos = ReadVec3(trans.Value.Pos);
        result.scl = ReadVec3(trans.Value.Scale);
        result.rot = ReadQuat(trans.Value.Rot);
        return result;
    }

    FrameData ReadTimelineFrame(Frame? frame)
    {
        var result = new FrameData();
        result.isInstance = frame.Value.IsInstance;
        result.transform = ReadTransform(frame.Value.Trans);

        for (int i = 0; i < frame.Value.LinesLength; i++)
        {
            result.Lines.Add(ReadLine(frame.Value.Lines(i)));
        }

        return result;
    }

    BrushStyle ReadBrushStyle(BrushSettings? settings)
    {
        var result = new BrushStyle();
        result.brushMode = (BrushMode)(int)settings.Value.BrushMode;
        result.brushType = (BrushType)(int)settings.Value.BrushType;
        result.constantWidth = settings.Value.ConstantWidth;
        result.isFlat = settings.Value.IsFlat;
        result.isObjectSpaceTex = settings.Value.IsObjectSpaceTex;
        result.isOneSided = settings.Value.IsOneSided;
        result.isWeb = settings.Value.IsWeb;
        result.multiLine = settings.Value.MultiLine;
        result.taperOpacity = settings.Value.TaperOpacity;
        result.taperShape = settings.Value.TaperShape;
        result.textureIndex = settings.Value.TextureIndex;
        return result;
    }

    SerializableVector3 ReadVec3(Vec3 vec)
    {
        return new SerializableVector3(vec.X, vec.Y, vec.Z);
    }

    SerializableColor ReadColor(AnimVRData.Color col)
    {
        return new SerializableColor(col.R, col.G, col.B, col.A);
    }

    SerializableQuaternion ReadQuat(AnimVRData.Quaternion quat)
    {
        return new SerializableQuaternion(new UnityEngine.Quaternion(quat.X, quat.Y, quat.Z, quat.W));
    }

    LineData ReadLine(AnimVRData.Line? line)
    {
        var result = new LineData();

        result.transform = ReadTransform(line.Value.Trans);

        var settings = ReadBrushStyle(line.Value.Settings);
        settings.ApplyTo(result);

        result.Points = new List<SerializableVector3>(line.Value.PointsLength);
        result.widths = new List<float>(line.Value.WidthsLength);
        result.colors = new List<SerializableColor>(line.Value.ColorsLength);
        result.light = new List<float>(line.Value.LightsLength);
        result.rotations = new List<SerializableQuaternion>(line.Value.RotationsLength);
        result.cameraOrientations = new List<SerializableQuaternion>(line.Value.CamOrientationsLength);

        for (int i = 0; i < line.Value.PointsLength; i++)
        {
            result.Points.Add(ReadVec3(line.Value.Points(i).Value));
            result.widths.Add(line.Value.Widths(i));
            result.colors.Add(ReadColor(line.Value.Colors(i).Value));
            result.light.Add(line.Value.Lights(i));
            result.rotations.Add(ReadQuat(line.Value.Rotations(i).Value));
            result.cameraOrientations.Add(ReadQuat(line.Value.CamOrientations(i).Value));
        }

        return result;
    }
    #endregion

    #region Export
    public override void Export(StageData stage, string path)
    {
        var builder = new FlatBufferBuilder(1024);

        var symbols = new Offset<Symbol>[stage.Symbols.Count];

        for(int i = 0; i < symbols.Length; i++)
        {
            symbols[i] = MakeSymbol(builder, stage.Symbols[i]);
        }

        var symbolsVector = Stage.CreateSymbolsVector(builder, symbols);


        Stage.StartStage(builder);
        Stage.AddSymbols(builder, symbolsVector);
        Stage.AddTrans(builder, MakeTransform(builder, stage.transform));
        var flatStage = Stage.EndStage(builder);

        builder.Finish(flatStage.Value);

        File.WriteAllBytes(path, builder.SizedByteArray());

    }

    Offset<AnimVRData.Transform> MakeTransform(FlatBufferBuilder builder, SerializableTransform transform)
    {
        return AnimVRData.Transform.CreateTransform(builder,
            transform.pos.x, transform.pos.y, transform.pos.z,
            transform.rot.x, transform.rot.y, transform.rot.z, transform.rot.w,
            transform.scl.x, transform.scl.y, transform.scl.z);
    }

    Offset<Playable> MakePlayable(FlatBufferBuilder builder, PlayableData playable)
    {
        var displayName = builder.CreateString(playable.displayName ??  "Stage");

        Playable.StartPlayable(builder);
        Playable.AddExpandedInLayerList(builder, playable.expandedInLayerList);
        Playable.AddIndexInSymbol(builder, playable.IndexInSymbol);
        Playable.AddIsVisible(builder, playable.isVisible);
        Playable.AddOpacity(builder, playable.opacity);
        Playable.AddTrans(builder, MakeTransform(builder, playable.transform));
        Playable.AddDisplayName(builder, displayName);
        return Playable.EndPlayable(builder);
    }

    Offset<Symbol> MakeSymbol(FlatBufferBuilder builder, SymbolData symbol)
    {
        var playableData = MakePlayable(builder, symbol);

        List<Offset<Symbol>> symbols = new List<Offset<Symbol>>();
        List<Offset<Timeline>> timelines = new List<Offset<Timeline>>();

        foreach(var playable in symbol.Playables)
        {
            if (playable is SymbolData) symbols.Add(MakeSymbol(builder, playable as SymbolData));
            else if (playable is TimeLineData) timelines.Add(MakeTimeline(builder, playable as TimeLineData));
        }

        var symbolsVector = Symbol.CreateSymbolsVector(builder, symbols.ToArray());
        var timelinesVector = Symbol.CreateTimelinesVector(builder, timelines.ToArray());

        Symbol.StartSymbol(builder);
        Symbol.AddBase(builder, playableData);
        Symbol.AddSymbols(builder, symbolsVector);
        Symbol.AddTimelines(builder, timelinesVector);
        return Symbol.EndSymbol(builder);
    }

    Offset<Timeline> MakeTimeline(FlatBufferBuilder builder, TimeLineData timeline)
    {
        var playablData = MakePlayable(builder, timeline);

        Offset<Frame>[] frames = new Offset<Frame>[timeline.Frames.Count];

        for(int i = 0; i < frames.Length; i++)
        {
            frames[i] = MakeTimelineFrame(builder, timeline.Frames[i]);
        }

        var framesVector = Timeline.CreateFramesVector(builder, frames);

        Timeline.StartTimeline(builder);
        Timeline.AddBase(builder, playablData);
        Timeline.AddFrames(builder, framesVector);
        return Timeline.EndTimeline(builder);
    }

    Offset<Frame> MakeTimelineFrame(FlatBufferBuilder builder, FrameData frameData)
    {

        Offset<AnimVRData.Line>[] lines = new Offset<AnimVRData.Line>[frameData.Lines.Count];

        for(int i = 0; i < lines.Length; i++)
        {
            lines[i] = MakeLine(builder, frameData.Lines[i]);
        }

        var linesVector = Frame.CreateLinesVector(builder, lines);

        Frame.StartFrame(builder);
        Frame.AddTrans(builder, MakeTransform(builder, frameData.transform));
        Frame.AddIsInstance(builder, frameData.isInstance);
        Frame.AddLines(builder, linesVector);
        return Frame.EndFrame(builder);
    }

    Offset<BrushSettings> MakeBrushSettings(FlatBufferBuilder builder, LineData lineData)
    {
        return BrushSettings.CreateBrushSettings(builder, (AnimVRData.BrushType)(int)lineData.brushType, (AnimVRData.BrushMode)(int)lineData.brushMode, lineData.isOneSided, lineData.isFlat, lineData.taperOpacity, lineData.taperShape, lineData.constantWidth, lineData.multiLine, lineData.isWeb, lineData.isObjectSpaceTex, lineData.textureIndex);
    }

    Offset<AnimVRData.Line> MakeLine(FlatBufferBuilder builder, LineData lineData)
    {
        var lightsVector = AnimVRData.Line.CreateLightsVector(builder, lineData.light.ToArray());
        var widthsVector = AnimVRData.Line.CreateWidthsVector(builder, lineData.widths.ToArray());

        AnimVRData.Line.StartColorsVector(builder, lineData.colors.Count);
        for (int i = 0; i < lineData.colors.Count; i++)
        {
            AnimVRData.Color.CreateColor(builder, lineData.colors[i].r, lineData.colors[i].g, lineData.colors[i].b, lineData.colors[i].a);
        }
        var colorsVector = builder.EndVector();

        AnimVRData.Line.StartPointsVector(builder, lineData.Points.Count);
        for (int i = 0; i < lineData.Points.Count; i++)
        {
            Vec3.CreateVec3(builder, lineData.Points[i].x, lineData.Points[i].y, lineData.Points[i].z);
        }
        var pointsVector = builder.EndVector();

        AnimVRData.Line.StartRotationsVector(builder, lineData.rotations.Count);
        for (int i = 0; i < lineData.rotations.Count; i++)
        {
            AnimVRData.Quaternion.CreateQuaternion(builder, lineData.rotations[i].x, lineData.rotations[i].y, lineData.rotations[i].z, lineData.rotations[i].w);
        }
        var rotVector = builder.EndVector();

        AnimVRData.Line.StartCamOrientationsVector(builder, lineData.cameraOrientations.Count);
        for (int i = 0; i < lineData.cameraOrientations.Count; i++)
        {
            AnimVRData.Quaternion.CreateQuaternion(builder, lineData.cameraOrientations[i].x, lineData.cameraOrientations[i].y, lineData.cameraOrientations[i].z, lineData.cameraOrientations[i].w);
        }
        var camOrientationsVector = builder.EndVector();

        AnimVRData.Line.StartLine(builder);
        AnimVRData.Line.AddTrans(builder, MakeTransform(builder, lineData.transform));
        AnimVRData.Line.AddLights(builder, lightsVector);
        AnimVRData.Line.AddWidths(builder, widthsVector);
        AnimVRData.Line.AddColors(builder, colorsVector);
        AnimVRData.Line.AddPoints(builder, pointsVector);
        AnimVRData.Line.AddRotations(builder, rotVector);
        AnimVRData.Line.AddCamOrientations(builder, camOrientationsVector);
        AnimVRData.Line.AddSettings(builder, MakeBrushSettings(builder, lineData));
        return AnimVRData.Line.EndLine(builder);
    }

    #endregion
}
