using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text.Json;
using WanderSpire.Scripting;

namespace SceneEditor.Services
{
    /// <summary>
    /// Manages command execution and undo/redo functionality
    /// </summary>
    public class CommandService : ReactiveObject
    {
        private readonly EditorEngine _engine;
        private readonly List<ICommand> _commandHistory = new();
        private int _currentCommandIndex = -1;
        private const int MaxHistorySize = 100;

        private bool _canUndo = false;
        private bool _canRedo = false;

        public bool CanUndo
        {
            get => _canUndo;
            private set => this.RaiseAndSetIfChanged(ref _canUndo, value);
        }

        public bool CanRedo
        {
            get => _canRedo;
            private set => this.RaiseAndSetIfChanged(ref _canRedo, value);
        }

        public event EventHandler? CommandExecuted;
        public event EventHandler? HistoryChanged;

        public CommandService(EditorEngine engine)
        {
            _engine = engine;
        }

        /// <summary>
        /// Execute a command and add it to history
        /// </summary>
        public void ExecuteCommand(ICommand command)
        {
            try
            {
                if (command.Execute())
                {
                    // Remove any commands after current position (when redoing after undo)
                    if (_currentCommandIndex < _commandHistory.Count - 1)
                    {
                        _commandHistory.RemoveRange(_currentCommandIndex + 1,
                            _commandHistory.Count - _currentCommandIndex - 1);
                    }

                    // Add command to history
                    _commandHistory.Add(command);
                    _currentCommandIndex++;

                    // Limit history size
                    if (_commandHistory.Count > MaxHistorySize)
                    {
                        _commandHistory.RemoveAt(0);
                        _currentCommandIndex--;
                    }

                    UpdateCanUndoRedo();
                    CommandExecuted?.Invoke(this, EventArgs.Empty);
                    HistoryChanged?.Invoke(this, EventArgs.Empty);

                    Console.WriteLine($"[CommandService] Executed command: {command.Description}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[CommandService] Command execution failed: {ex}");
            }
        }

        /// <summary>
        /// Undo the last command
        /// </summary>
        public bool Undo()
        {
            try
            {
                if (!CanUndo || _currentCommandIndex < 0)
                    return false;

                var command = _commandHistory[_currentCommandIndex];
                if (command.Undo())
                {
                    _currentCommandIndex--;
                    UpdateCanUndoRedo();
                    HistoryChanged?.Invoke(this, EventArgs.Empty);

                    Console.WriteLine($"[CommandService] Undone command: {command.Description}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[CommandService] Undo failed: {ex}");
            }

            return false;
        }

        /// <summary>
        /// Redo the next command
        /// </summary>
        public bool Redo()
        {
            try
            {
                if (!CanRedo || _currentCommandIndex >= _commandHistory.Count - 1)
                    return false;

                _currentCommandIndex++;
                var command = _commandHistory[_currentCommandIndex];

                if (command.Execute())
                {
                    UpdateCanUndoRedo();
                    HistoryChanged?.Invoke(this, EventArgs.Empty);

                    Console.WriteLine($"[CommandService] Redone command: {command.Description}");
                    return true;
                }
                else
                {
                    _currentCommandIndex--; // Revert on failure
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[CommandService] Redo failed: {ex}");
            }

            return false;
        }

        /// <summary>
        /// Clear command history
        /// </summary>
        public void ClearHistory()
        {
            _commandHistory.Clear();
            _currentCommandIndex = -1;
            UpdateCanUndoRedo();
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Get command history for display
        /// </summary>
        public IReadOnlyList<ICommand> GetHistory()
        {
            return _commandHistory.AsReadOnly();
        }

        /// <summary>
        /// Get current command index
        /// </summary>
        public int CurrentCommandIndex => _currentCommandIndex;

        private void UpdateCanUndoRedo()
        {
            CanUndo = _currentCommandIndex >= 0;
            CanRedo = _currentCommandIndex < _commandHistory.Count - 1;
        }

        /// <summary>
        /// Create a tile painting command
        /// </summary>
        public void ExecuteTilePaintCommand(EntityId tilemapLayer, int tileX, int tileY, int newTileId)
        {
            var command = new TilePaintCommand(_engine, tilemapLayer, tileX, tileY, newTileId);
            ExecuteCommand(command);
        }

        /// <summary>
        /// Create an entity move command
        /// </summary>
        public void ExecuteEntityMoveCommand(EntityId entity, float deltaX, float deltaY)
        {
            var command = new EntityMoveCommand(_engine, entity, deltaX, deltaY);
            ExecuteCommand(command);
        }

        /// <summary>
        /// Create an entity delete command
        /// </summary>
        public void ExecuteEntityDeleteCommand(EntityId entity)
        {
            var command = new EntityDeleteCommand(_engine, entity);
            ExecuteCommand(command);
        }
    }

    /// <summary>
    /// Interface for undoable commands
    /// </summary>
    public interface ICommand
    {
        string Description { get; }
        bool Execute();
        bool Undo();
    }

    /// <summary>
    /// Command for painting tiles
    /// </summary>
    public class TilePaintCommand : ICommand
    {
        private readonly EditorEngine _engine;
        private readonly EntityId _tilemapLayer;
        private readonly int _tileX;
        private readonly int _tileY;
        private readonly int _newTileId;
        private int _oldTileId;

        public string Description => $"Paint Tile ({_tileX}, {_tileY}) = {_newTileId}";

        public TilePaintCommand(EditorEngine engine, EntityId tilemapLayer, int tileX, int tileY, int newTileId)
        {
            _engine = engine;
            _tilemapLayer = tilemapLayer;
            _tileX = tileX;
            _tileY = tileY;
            _newTileId = newTileId;
        }

        public bool Execute()
        {
            try
            {
                // Store old tile ID for undo
                _oldTileId = TilemapInterop.Tilemap_GetTile(_engine.Context, _tilemapLayer, _tileX, _tileY);

                // Set new tile
                TilemapInterop.Tilemap_SetTile(_engine.Context, _tilemapLayer, _tileX, _tileY, _newTileId);
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilePaintCommand] Execute failed: {ex}");
                return false;
            }
        }

        public bool Undo()
        {
            try
            {
                TilemapInterop.Tilemap_SetTile(_engine.Context, _tilemapLayer, _tileX, _tileY, _oldTileId);
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilePaintCommand] Undo failed: {ex}");
                return false;
            }
        }
    }

    /// <summary>
    /// Command for moving entities
    /// </summary>
    public class EntityMoveCommand : ICommand
    {
        private readonly EditorEngine _engine;
        private readonly EntityId _entity;
        private readonly float _deltaX;
        private readonly float _deltaY;

        public string Description => $"Move Entity {_entity.id}";

        public EntityMoveCommand(EditorEngine engine, EntityId entity, float deltaX, float deltaY)
        {
            _engine = engine;
            _entity = entity;
            _deltaX = deltaX;
            _deltaY = deltaY;
        }

        public bool Execute()
        {
            try
            {
                // Get current position
                EngineInterop.Engine_GetEntityWorldPosition(_engine.Context, _entity, out float currentX, out float currentY);

                // Set new position
                var newX = currentX + _deltaX;
                var newY = currentY + _deltaY;

                // Update transform component
                var transformJson = JsonSerializer.Serialize(new
                {
                    localPosition = new[] { newX, newY },
                    worldPosition = new[] { newX, newY },
                    localRotation = 0.0f,
                    worldRotation = 0.0f,
                    localScale = new[] { 1.0f, 1.0f },
                    worldScale = new[] { 1.0f, 1.0f },
                    isDirty = true
                });

                return EngineInterop.SetComponentJson(_engine.Context, _entity, "TransformComponent", transformJson) == 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[EntityMoveCommand] Execute failed: {ex}");
                return false;
            }
        }

        public bool Undo()
        {
            try
            {
                // Move back by negative delta
                return new EntityMoveCommand(_engine, _entity, -_deltaX, -_deltaY).Execute();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[EntityMoveCommand] Undo failed: {ex}");
                return false;
            }
        }
    }

    /// <summary>
    /// Command for deleting entities
    /// </summary>
    public class EntityDeleteCommand : ICommand
    {
        private readonly EditorEngine _engine;
        private readonly EntityId _entity;
        private string _entityData = string.Empty;

        public string Description => $"Delete Entity {_entity.id}";

        public EntityDeleteCommand(EditorEngine engine, EntityId entity)
        {
            _engine = engine;
            _entity = entity;
        }

        public bool Execute()
        {
            try
            {
                // Store entity data for undo (simplified - in full implementation would serialize all components)
                _entityData = EngineInterop.GetComponentJsonSafe(_engine.Context, _entity, "TagComponent");

                // Delete entity
                EngineInterop.DestroyEntity(_engine.Context, _entity);
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[EntityDeleteCommand] Execute failed: {ex}");
                return false;
            }
        }

        public bool Undo()
        {
            try
            {
                // Create new entity (simplified - full implementation would restore all components)
                var newEntity = EngineInterop.CreateEntity(_engine.Context);
                if (!string.IsNullOrEmpty(_entityData))
                {
                    EngineInterop.SetComponentJsonSafe(_engine.Context, newEntity, "TagComponent", _entityData);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[EntityDeleteCommand] Undo failed: {ex}");
                return false;
            }
        }
    }
}