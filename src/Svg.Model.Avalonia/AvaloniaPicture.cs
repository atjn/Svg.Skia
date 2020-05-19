﻿using System;
using System.Collections.Generic;
using A = Avalonia;
using AM = Avalonia.Media;
using AVMI = Avalonia.Visuals.Media.Imaging;

namespace Svg.Model.Avalonia
{
    public abstract class DrawCommand : IDisposable
    {
        public virtual void Dispose()
        {
        }
    }

    public class GeometryClipDrawCommand : DrawCommand
    {
        public readonly AM.Geometry? Clip;

        public GeometryClipDrawCommand(AM.Geometry? clip)
        {
            Clip = clip;
        }
    }

    public class ClipDrawCommand : DrawCommand
    {
        public readonly A.Rect Clip;

        public ClipDrawCommand(A.Rect clip)
        {
            Clip = clip;
        }
    }

    public class SaveDrawCommand : DrawCommand
    {
        public SaveDrawCommand()
        {
        }
    }

    public class RestoreDrawCommand : DrawCommand
    {
        public RestoreDrawCommand()
        {
        }
    }

    public class SetTransformDrawCommand : DrawCommand
    {
        public readonly A.Matrix Matrix;

        public SetTransformDrawCommand(A.Matrix matrix)
        {
            Matrix = matrix;
        }
    }

    public class SaveLayerDrawCommand : DrawCommand
    {
        public SaveLayerDrawCommand()
        {
        }
    }

    public class ImageDrawCommand : DrawCommand
    {
        public readonly AM.IImage? Source;
        public readonly A.Rect SourceRect;
        public readonly A.Rect DestRect;
        public readonly AVMI.BitmapInterpolationMode BitmapInterpolationMode;

        public ImageDrawCommand(AM.IImage? source, A.Rect sourceRect, A.Rect destRect, AVMI.BitmapInterpolationMode bitmapInterpolationMode)
        {
            Source = source;
            SourceRect = sourceRect;
            DestRect = destRect;
            BitmapInterpolationMode = bitmapInterpolationMode;
        }

        public override void Dispose()
        {
            base.Dispose();

            if (Source is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    public class GeometryDrawCommand : DrawCommand
    {
        public readonly AM.IBrush? Brush;
        public readonly AM.IPen? Pen;
        public readonly AM.Geometry? Geometry;

        public GeometryDrawCommand(AM.IBrush? brush, AM.IPen? pen, AM.Geometry? geometry)
        {
            Brush = brush;
            Pen = pen;
            Geometry = geometry;
        }
    }

    public class LineDrawCommand : DrawCommand
    {
        public readonly AM.IPen? Pen;
        public readonly A.Point P1;
        public readonly A.Point P2;

        public LineDrawCommand(AM.IPen? pen, A.Point p1, A.Point p2)
        {
            Pen = pen;
            P1 = p1;
            P2 = p2;
        }
    }

    public class RectangleDrawCommand : DrawCommand
    {
        public readonly AM.IBrush? Brush;
        public readonly AM.IPen? Pen;
        public readonly A.Rect Rect;
        public readonly double RadiusX;
        public readonly double RadiusY;

        public RectangleDrawCommand(AM.IBrush? brush, AM.IPen? pen, A.Rect rect, double radiusX, double radiusY)
        {
            Brush = brush;
            Pen = pen;
            Rect = rect;
            RadiusX = radiusX;
            RadiusY = radiusY;
        }
    }

    public class TextDrawCommand : DrawCommand
    {
        public readonly AM.IBrush? Brush;
        public readonly A.Point Origin;
        public readonly AM.FormattedText? FormattedText;

        public TextDrawCommand(AM.IBrush? brush, A.Point origin, AM.FormattedText? formattedText)
        {
            Brush = brush;
            Origin = origin;
            FormattedText = formattedText;
        }
    }

    public class AvaloniaPicture : IDisposable
    {
        public readonly IList<DrawCommand>? Commands;

        public AvaloniaPicture()
        {
            Commands = new List<DrawCommand>();
        }

        public void Dispose()
        {
            if (Commands == null)
            {
                return;
            }

            foreach (var command in Commands)
            {
                command.Dispose();
            }
        }

        public void Draw(AM.DrawingContext context)
        {
            if (Commands == null)
            {
                return;
            }

            using var transformContainerState = context.PushTransformContainer();

            var pushedStates = new Stack<Stack<IDisposable>>();

            foreach (var command in Commands)
            {
                switch (command)
                {
                    case GeometryClipDrawCommand geometryClipDrawCommand:
                        {
                            var geometryPushedState = context.PushGeometryClip(geometryClipDrawCommand.Clip);
                            var currentPushedStates = pushedStates.Peek();
                            currentPushedStates.Push(geometryPushedState);
                        }
                        break;
                    case ClipDrawCommand clipDrawCommand:
                        {
                            var clipPushedState = context.PushClip(clipDrawCommand.Clip);
                            var currentPushedStates = pushedStates.Peek();
                            currentPushedStates.Push(clipPushedState);
                        }
                        break;
                    case SaveDrawCommand _:
                        {
                            pushedStates.Push(new Stack<IDisposable>());
                        }
                        break;
                    case RestoreDrawCommand _:
                        {
                            var currentPushedStates = pushedStates.Pop();
                            while (currentPushedStates.Count > 0)
                            {
                                var pushedState = currentPushedStates.Pop();
                                pushedState.Dispose();
                            }
                        }
                        break;
                    case SetTransformDrawCommand setTransformDrawCommand:
                        {
                            var transformPreTransform = context.PushSetTransform(setTransformDrawCommand.Matrix);
                            var currentPushedStates = pushedStates.Peek();
                            currentPushedStates.Push(transformPreTransform);
                        }
                        break;
                    case SaveLayerDrawCommand saveLayerDrawCommand:
                        {
                            pushedStates.Push(new Stack<IDisposable>());
                        }
                        break;
                    case ImageDrawCommand imageDrawCommand:
                        {
                            context.DrawImage(
                                imageDrawCommand.Source,
                                imageDrawCommand.SourceRect,
                                imageDrawCommand.DestRect,
                                imageDrawCommand.BitmapInterpolationMode);
                        }
                        break;
                    case GeometryDrawCommand geometryDrawCommand:
                        {
                            context.DrawGeometry(
                                geometryDrawCommand.Brush,
                                geometryDrawCommand.Pen,
                                geometryDrawCommand.Geometry);
                        }
                        break;
                    case LineDrawCommand lineDrawCommand:
                        {
                            context.DrawLine(
                                lineDrawCommand.Pen,
                                lineDrawCommand.P1,
                                lineDrawCommand.P2);
                        }
                        break;
                    case RectangleDrawCommand rectangleDrawCommand:
                        {
                            context.DrawRectangle(
                                rectangleDrawCommand.Brush,
                                rectangleDrawCommand.Pen,
                                rectangleDrawCommand.Rect,
                                rectangleDrawCommand.RadiusX,
                                rectangleDrawCommand.RadiusY);
                        }
                        break;
                    case TextDrawCommand textDrawCommand:
                        {
                            context.DrawText(
                                textDrawCommand.Brush,
                                textDrawCommand.Origin,
                                textDrawCommand.FormattedText);
                        }
                        break;
                }
            }
        }
    }
}