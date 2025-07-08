using Avalonia;
using Avalonia.Media;
using SceneEditor.Models;
using SceneEditor.ViewModels;
using System;
using System.Collections.Generic;

namespace SceneEditor.Tools.Gizmos
{
    /// <summary>
    /// Interface for all gizmo types that can manipulate entities
    /// </summary>
    public interface IGizmo
    {
        string Name { get; }
        bool IsActive { get; set; }
        bool IsVisible { get; set; }

        void SetTargets(IEnumerable<SceneNode> targets);
        void UpdateFromTargets();
        void Render(DrawingContext context, Matrix transform);

        GizmoHitResult HitTest(Point screenPoint, Matrix viewTransform);
        void BeginManipulation(GizmoHandle handle, Point screenPoint);
        void UpdateManipulation(Point screenPoint, ViewportInputModifiers modifiers);
        void EndManipulation();

        event System.EventHandler<GizmoManipulationEventArgs>? ManipulationStarted;
        event System.EventHandler<GizmoManipulationEventArgs>? ManipulationUpdated;
        event System.EventHandler<GizmoManipulationEventArgs>? ManipulationCompleted;
    }

    /// <summary>
    /// Base gizmo class with common functionality
    /// </summary>
    public abstract class GizmoBase : IGizmo
    {
        protected List<SceneNode> _targets = new();
        protected Point _manipulationStart;
        protected GizmoHandle? _activeHandle;
        protected bool _isManipulating;

        public abstract string Name { get; }
        public bool IsActive { get; set; } = true;
        public bool IsVisible { get; set; } = true;

        public event System.EventHandler<GizmoManipulationEventArgs>? ManipulationStarted;
        public event System.EventHandler<GizmoManipulationEventArgs>? ManipulationUpdated;
        public event System.EventHandler<GizmoManipulationEventArgs>? ManipulationCompleted;

        public virtual void SetTargets(IEnumerable<SceneNode> targets)
        {
            _targets.Clear();
            _targets.AddRange(targets);
            UpdateFromTargets();
        }

        public abstract void UpdateFromTargets();
        public abstract void Render(DrawingContext context, Matrix transform);
        public abstract GizmoHitResult HitTest(Point screenPoint, Matrix viewTransform);

        public virtual void BeginManipulation(GizmoHandle handle, Point screenPoint)
        {
            _activeHandle = handle;
            _manipulationStart = screenPoint;
            _isManipulating = true;

            ManipulationStarted?.Invoke(this, new GizmoManipulationEventArgs
            {
                Handle = handle,
                StartPosition = screenPoint,
                Targets = _targets
            });
        }

        public abstract void UpdateManipulation(Point screenPoint, ViewportInputModifiers modifiers);

        public virtual void EndManipulation()
        {
            if (!_isManipulating) return;

            ManipulationCompleted?.Invoke(this, new GizmoManipulationEventArgs
            {
                Handle = _activeHandle,
                Targets = _targets
            });

            _activeHandle = null;
            _isManipulating = false;
        }

        protected void OnManipulationUpdated(Point currentPosition)
        {
            ManipulationUpdated?.Invoke(this, new GizmoManipulationEventArgs
            {
                Handle = _activeHandle,
                StartPosition = _manipulationStart,
                CurrentPosition = currentPosition,
                Targets = _targets
            });
        }
    }

    /// <summary>
    /// Gizmo handle types for different manipulation modes
    /// </summary>
    public enum GizmoHandle
    {
        None,
        Center,
        XAxis,
        YAxis,
        XYPlane,
        RotationRing,
        ScaleX,
        ScaleY,
        ScaleXY,
        Corner_TopLeft,
        Corner_TopRight,
        Corner_BottomLeft,
        Corner_BottomRight,
        Edge_Top,
        Edge_Bottom,
        Edge_Left,
        Edge_Right
    }

    /// <summary>
    /// Result of gizmo hit testing
    /// </summary>
    public class GizmoHitResult
    {
        public bool Hit { get; set; }
        public GizmoHandle Handle { get; set; }
        public float Distance { get; set; }
        public Point HitPoint { get; set; }

        public static GizmoHitResult Miss = new() { Hit = false };
    }

    /// <summary>
    /// Event args for gizmo manipulation events
    /// </summary>
    public class GizmoManipulationEventArgs : System.EventArgs
    {
        public GizmoHandle? Handle { get; set; }
        public Point StartPosition { get; set; }
        public Point CurrentPosition { get; set; }
        public List<SceneNode> Targets { get; set; } = new();
        public Vector Delta => CurrentPosition - StartPosition;
    }

    /// <summary>
    /// Translation gizmo for moving entities
    /// </summary>
    public class TranslationGizmo : GizmoBase
    {
        private Point _center;
        private readonly Pen _xAxisPen = new((Brush)Brushes.Red, 3);
        private readonly Pen _yAxisPen = new((Brush)Brushes.Green, 3);
        private readonly Pen _centerPen = new((Brush)Brushes.Yellow, 2);
        private readonly Brush _centerBrush = (Brush)Brushes.Yellow;
        private readonly float _axisLength = 60f;
        private readonly float _handleSize = 8f;

        public override string Name => "Translation";

        public override void UpdateFromTargets()
        {
            if (_targets.Count == 0)
            {
                IsVisible = false;
                return;
            }

            IsVisible = true;

            // Calculate center position from all targets
            float totalX = 0, totalY = 0;
            foreach (var target in _targets)
            {
                if (target.Entity?.IsValid == true)
                {
                    try
                    {
                        var x = target.Entity.GetField<float>("TransformComponent", "localPosition[0]");
                        var y = target.Entity.GetField<float>("TransformComponent", "localPosition[1]");
                        totalX += x;
                        totalY += y;
                    }
                    catch { }
                }
            }

            _center = new Point(totalX / _targets.Count, totalY / _targets.Count);
        }

        public override void Render(DrawingContext context, Matrix transform)
        {
            if (!IsVisible || !IsActive) return;

            var screenCenter = _center * transform;

            // Draw X axis (red)
            var xEnd = new Point(_center.X + _axisLength, _center.Y) * transform;
            context.DrawLine(_xAxisPen, screenCenter, xEnd);

            // Draw X axis arrow head
            DrawArrowHead(context, _xAxisPen.Brush, screenCenter, xEnd);

            // Draw Y axis (green)
            var yEnd = new Point(_center.X, _center.Y + _axisLength) * transform;
            context.DrawLine(_yAxisPen, screenCenter, yEnd);

            // Draw Y axis arrow head
            DrawArrowHead(context, _yAxisPen.Brush, screenCenter, yEnd);

            // Draw center handle
            context.DrawEllipse(_centerBrush, _centerPen, screenCenter, _handleSize, _handleSize);
        }

        private void DrawArrowHead(DrawingContext context, IBrush brush, Point start, Point end)
        {
            var direction = (Vector)(end - start);
            var length = direction.Length;
            if (length < 1) return;

            direction = direction / length;
            var perpendicular = new Vector(-direction.Y, direction.X);

            var arrowSize = 10;
            var arrowBack = end - direction * arrowSize;
            var arrow1 = arrowBack + perpendicular * (arrowSize * 0.5);
            var arrow2 = arrowBack - perpendicular * (arrowSize * 0.5);

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(end, true);
                ctx.LineTo(arrow1);
                ctx.LineTo(arrow2);
                ctx.EndFigure(true);
            }

            context.DrawGeometry((Brush)brush, null, geometry);
        }

        public override GizmoHitResult HitTest(Point screenPoint, Matrix viewTransform)
        {
            if (!IsVisible || !IsActive) return GizmoHitResult.Miss;

            var screenCenter = _center * viewTransform;
            var threshold = 10; // Hit test threshold in pixels

            // Test center handle
            if (((Vector)(screenPoint - screenCenter)).Length <= _handleSize + threshold)
            {
                return new GizmoHitResult
                {
                    Hit = true,
                    Handle = GizmoHandle.XYPlane,
                    Distance = (float)((Vector)(screenPoint - screenCenter)).Length,
                    HitPoint = screenPoint
                };
            }

            // Test X axis
            var xEnd = new Point(_center.X + _axisLength, _center.Y) * viewTransform;
            if (DistanceToLine(screenPoint, screenCenter, xEnd) <= threshold)
            {
                return new GizmoHitResult
                {
                    Hit = true,
                    Handle = GizmoHandle.XAxis,
                    Distance = (float)DistanceToLine(screenPoint, screenCenter, xEnd),
                    HitPoint = screenPoint
                };
            }

            // Test Y axis
            var yEnd = new Point(_center.X, _center.Y + _axisLength) * viewTransform;
            if (DistanceToLine(screenPoint, screenCenter, yEnd) <= threshold)
            {
                return new GizmoHitResult
                {
                    Hit = true,
                    Handle = GizmoHandle.YAxis,
                    Distance = (float)DistanceToLine(screenPoint, screenCenter, yEnd),
                    HitPoint = screenPoint
                };
            }

            return GizmoHitResult.Miss;
        }

        private double DistanceToLine(Point point, Point lineStart, Point lineEnd)
        {
            var line = (Vector)(lineEnd - lineStart);
            var lineLength = line.Length;
            if (lineLength < 1) return ((Vector)(point - lineStart)).Length;

            var normalized = line / lineLength;
            var toPoint = (Vector)(point - lineStart);
            var projection = Vector.Dot(toPoint, normalized);

            if (projection <= 0) return ((Vector)(point - lineStart)).Length;
            if (projection >= lineLength) return ((Vector)(point - lineEnd)).Length;

            var closestPoint = lineStart + normalized * projection;
            return ((Vector)(point - closestPoint)).Length;
        }

        public override void UpdateManipulation(Point screenPoint, ViewportInputModifiers modifiers)
        {
            if (!_isManipulating || _activeHandle == null) return;

            var delta = (Vector)(screenPoint - _manipulationStart);

            // Apply constraint based on handle type
            switch (_activeHandle)
            {
                case GizmoHandle.XAxis:
                    delta = new Vector(delta.X, 0);
                    break;
                case GizmoHandle.YAxis:
                    delta = new Vector(0, delta.Y);
                    break;
                case GizmoHandle.XYPlane:
                    // No constraint
                    break;
            }

            // Apply grid snapping if enabled
            if (modifiers.HasFlag(ViewportInputModifiers.Control))
            {
                var gridSize = 32f; // Get from settings
                delta = new Vector(
                    Math.Round(delta.X / gridSize) * gridSize,
                    Math.Round(delta.Y / gridSize) * gridSize
                );
            }

            // Update target positions
            foreach (var target in _targets)
            {
                if (target.Entity?.IsValid == true)
                {
                    try
                    {
                        var currentX = target.Entity.GetField<float>("TransformComponent", "localPosition[0]");
                        var currentY = target.Entity.GetField<float>("TransformComponent", "localPosition[1]");

                        target.Entity.SetField("TransformComponent", "localPosition[0]", currentX + (float)delta.X);
                        target.Entity.SetField("TransformComponent", "localPosition[1]", currentY + (float)delta.Y);
                    }
                    catch (Exception ex)
                    {
                        System.Console.Error.WriteLine($"Failed to update entity position: {ex.Message}");
                    }
                }
            }

            OnManipulationUpdated(screenPoint);
            UpdateFromTargets();
        }
    }

    /// <summary>
    /// Rotation gizmo for rotating entities
    /// </summary>
    public class RotationGizmo : GizmoBase
    {
        private Point _center;
        private readonly Pen _ringPen = new((Brush)Brushes.Blue, 2);
        private readonly float _radius = 50f;

        public override string Name => "Rotation";

        public override void UpdateFromTargets()
        {
            if (_targets.Count == 0)
            {
                IsVisible = false;
                return;
            }

            IsVisible = true;

            // Calculate center position
            float totalX = 0, totalY = 0;
            foreach (var target in _targets)
            {
                if (target.Entity?.IsValid == true)
                {
                    try
                    {
                        var x = target.Entity.GetField<float>("TransformComponent", "localPosition[0]");
                        var y = target.Entity.GetField<float>("TransformComponent", "localPosition[1]");
                        totalX += x;
                        totalY += y;
                    }
                    catch { }
                }
            }

            _center = new Point(totalX / _targets.Count, totalY / _targets.Count);
        }

        public override void Render(DrawingContext context, Matrix transform)
        {
            if (!IsVisible || !IsActive) return;

            var screenCenter = _center * transform;
            context.DrawEllipse(null, _ringPen, screenCenter, _radius, _radius);
        }

        public override GizmoHitResult HitTest(Point screenPoint, Matrix viewTransform)
        {
            if (!IsVisible || !IsActive) return GizmoHitResult.Miss;

            var screenCenter = _center * viewTransform;
            var distance = ((Vector)(screenPoint - screenCenter)).Length;
            var threshold = 10;

            if (Math.Abs(distance - _radius) <= threshold)
            {
                return new GizmoHitResult
                {
                    Hit = true,
                    Handle = GizmoHandle.RotationRing,
                    Distance = (float)Math.Abs(distance - _radius),
                    HitPoint = screenPoint
                };
            }

            return GizmoHitResult.Miss;
        }

        public override void UpdateManipulation(Point screenPoint, ViewportInputModifiers modifiers)
        {
            if (!_isManipulating) return;

            var startVector = (Vector)(_manipulationStart - _center);
            var currentVector = (Vector)(screenPoint - _center);

            var angle = Math.Atan2(currentVector.Y, currentVector.X) - Math.Atan2(startVector.Y, startVector.X);

            // Apply angle snapping if holding shift
            if (modifiers.HasFlag(ViewportInputModifiers.Shift))
            {
                var snapAngle = Math.PI / 8; // 22.5 degrees
                angle = Math.Round(angle / snapAngle) * snapAngle;
            }

            // Update target rotations
            foreach (var target in _targets)
            {
                if (target.Entity?.IsValid == true)
                {
                    try
                    {
                        var currentRotation = target.Entity.GetField<float>("TransformComponent", "localRotation");
                        target.Entity.SetField("TransformComponent", "localRotation", currentRotation + (float)angle);
                    }
                    catch (Exception ex)
                    {
                        System.Console.Error.WriteLine($"Failed to update entity rotation: {ex.Message}");
                    }
                }
            }

            OnManipulationUpdated(screenPoint);
        }
    }

    /// <summary>
    /// Scale gizmo for scaling entities
    /// </summary>
    public class ScaleGizmo : GizmoBase
    {
        private Point _center;
        private readonly Pen _handlePen = new((Brush)Brushes.Blue, 2);
        private readonly Brush _handleBrush = (Brush)Brushes.LightBlue;
        private readonly float _handleSize = 6f;
        private readonly float _axisLength = 40f;

        public override string Name => "Scale";

        public override void UpdateFromTargets()
        {
            if (_targets.Count == 0)
            {
                IsVisible = false;
                return;
            }

            IsVisible = true;

            // Calculate center position
            float totalX = 0, totalY = 0;
            foreach (var target in _targets)
            {
                if (target.Entity?.IsValid == true)
                {
                    try
                    {
                        var x = target.Entity.GetField<float>("TransformComponent", "localPosition[0]");
                        var y = target.Entity.GetField<float>("TransformComponent", "localPosition[1]");
                        totalX += x;
                        totalY += y;
                    }
                    catch { }
                }
            }

            _center = new Point(totalX / _targets.Count, totalY / _targets.Count);
        }

        public override void Render(DrawingContext context, Matrix transform)
        {
            if (!IsVisible || !IsActive) return;

            var screenCenter = _center * transform;

            // Draw scale handles
            var xHandle = new Point(_center.X + _axisLength, _center.Y) * transform;
            var yHandle = new Point(_center.X, _center.Y + _axisLength) * transform;
            var xyHandle = new Point(_center.X + _axisLength * 0.7, _center.Y + _axisLength * 0.7) * transform;

            // Draw lines to handles
            context.DrawLine(_handlePen, screenCenter, xHandle);
            context.DrawLine(_handlePen, screenCenter, yHandle);

            // Draw handles
            context.DrawRectangle(_handleBrush, _handlePen,
                new Rect(xHandle.X - _handleSize / 2, xHandle.Y - _handleSize / 2, _handleSize, _handleSize));
            context.DrawRectangle(_handleBrush, _handlePen,
                new Rect(yHandle.X - _handleSize / 2, yHandle.Y - _handleSize / 2, _handleSize, _handleSize));
            context.DrawRectangle(_handleBrush, _handlePen,
                new Rect(xyHandle.X - _handleSize / 2, xyHandle.Y - _handleSize / 2, _handleSize, _handleSize));
        }

        public override GizmoHitResult HitTest(Point screenPoint, Matrix viewTransform)
        {
            if (!IsVisible || !IsActive) return GizmoHitResult.Miss;

            var screenCenter = _center * viewTransform;
            var threshold = _handleSize + 5;

            // Test X scale handle
            var xHandle = new Point(_center.X + _axisLength, _center.Y) * viewTransform;
            if (((Vector)(screenPoint - xHandle)).Length <= threshold)
            {
                return new GizmoHitResult
                {
                    Hit = true,
                    Handle = GizmoHandle.ScaleX,
                    Distance = (float)((Vector)(screenPoint - xHandle)).Length,
                    HitPoint = screenPoint
                };
            }

            // Test Y scale handle
            var yHandle = new Point(_center.X, _center.Y + _axisLength) * viewTransform;
            if (((Vector)(screenPoint - yHandle)).Length <= threshold)
            {
                return new GizmoHitResult
                {
                    Hit = true,
                    Handle = GizmoHandle.ScaleY,
                    Distance = (float)((Vector)(screenPoint - yHandle)).Length,
                    HitPoint = screenPoint
                };
            }

            // Test uniform scale handle
            var xyHandle = new Point(_center.X + _axisLength * 0.7, _center.Y + _axisLength * 0.7) * viewTransform;
            if (((Vector)(screenPoint - xyHandle)).Length <= threshold)
            {
                return new GizmoHitResult
                {
                    Hit = true,
                    Handle = GizmoHandle.ScaleXY,
                    Distance = (float)((Vector)(screenPoint - xyHandle)).Length,
                    HitPoint = screenPoint
                };
            }

            return GizmoHitResult.Miss;
        }

        public override void UpdateManipulation(Point screenPoint, ViewportInputModifiers modifiers)
        {
            if (!_isManipulating || _activeHandle == null) return;

            var delta = (Vector)(screenPoint - _manipulationStart);
            var scaleFactor = 1.0f + (float)delta.Length * 0.01f;

            if (delta.Length > ((Vector)(_manipulationStart - _center)).Length)
                scaleFactor = 1.0f / scaleFactor;

            // Apply uniform scaling if holding shift or using XY handle
            bool uniformScale = modifiers.HasFlag(ViewportInputModifiers.Shift) || _activeHandle == GizmoHandle.ScaleXY;

            foreach (var target in _targets)
            {
                if (target.Entity?.IsValid == true)
                {
                    try
                    {
                        var currentScaleX = target.Entity.GetField<float>("TransformComponent", "localScale[0]");
                        var currentScaleY = target.Entity.GetField<float>("TransformComponent", "localScale[1]");

                        if (uniformScale || _activeHandle == GizmoHandle.ScaleX)
                            target.Entity.SetField("TransformComponent", "localScale[0]", currentScaleX * scaleFactor);

                        if (uniformScale || _activeHandle == GizmoHandle.ScaleY)
                            target.Entity.SetField("TransformComponent", "localScale[1]", currentScaleY * scaleFactor);
                    }
                    catch (Exception ex)
                    {
                        System.Console.Error.WriteLine($"Failed to update entity scale: {ex.Message}");
                    }
                }
            }

            OnManipulationUpdated(screenPoint);
        }
    }
}
