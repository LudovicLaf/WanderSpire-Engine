#include "WanderSpire/Editor/CommandHistory.h"
#include <spdlog/spdlog.h>

namespace WanderSpire {

	void CommandHistory::ExecuteCommand(std::unique_ptr<ICommand> command) {
		if (!command) {
			return;
		}

		// Try to merge with last command if possible
		if (!history.empty() && currentIndex >= 0) {
			auto& lastCommand = history[currentIndex];
			if (lastCommand->CanMerge(command.get())) {
				lastCommand->MergeWith(command.get());
				spdlog::debug("[CommandHistory] Merged command: {}", lastCommand->GetDescription());
				return;
			}
		}

		// Execute the command
		try {
			command->Execute();

			// Remove any commands after current index (we're branching)
			if (currentIndex + 1 < static_cast<int>(history.size())) {
				history.erase(history.begin() + currentIndex + 1, history.end());
			}

			// Add new command
			history.push_back(std::move(command));
			currentIndex = static_cast<int>(history.size()) - 1;

			// Trim history if it's too large
			TrimHistory();

			spdlog::debug("[CommandHistory] Executed command: {} (history size: {})",
				history[currentIndex]->GetDescription(), history.size());
		}
		catch (const std::exception& e) {
			spdlog::error("[CommandHistory] Command execution failed: {}", e.what());
		}
	}

	void CommandHistory::Undo() {
		if (!CanUndo()) {
			return;
		}

		try {
			auto& command = history[currentIndex];
			command->Undo();
			currentIndex--;

			spdlog::debug("[CommandHistory] Undid command: {}", command->GetDescription());
		}
		catch (const std::exception& e) {
			spdlog::error("[CommandHistory] Undo failed: {}", e.what());
		}
	}

	void CommandHistory::Redo() {
		if (!CanRedo()) {
			return;
		}

		try {
			currentIndex++;
			auto& command = history[currentIndex];
			command->Execute();

			spdlog::debug("[CommandHistory] Redid command: {}", command->GetDescription());
		}
		catch (const std::exception& e) {
			spdlog::error("[CommandHistory] Redo failed: {}", e.what());
			currentIndex--; // Rollback index change
		}
	}

	void CommandHistory::Clear() {
		history.clear();
		currentIndex = -1;
		spdlog::debug("[CommandHistory] Cleared command history");
	}

	void CommandHistory::SetMaxHistorySize(int size) {
		// Validate input
		if (size < 1) {
			spdlog::warn("[CommandHistory] Invalid max history size: {}, using minimum of 1", size);
			size = 1;
		}

		if (size > 10000) {
			spdlog::warn("[CommandHistory] Very large history size: {}, this may use significant memory", size);
		}

		int oldSize = maxHistorySize;
		maxHistorySize = size;

		// If reducing size, trim excess commands but preserve current position if possible
		if (size < oldSize) {
			int excessCommands = static_cast<int>(history.size()) - maxHistorySize;
			if (excessCommands > 0) {
				// Remove oldest commands first
				history.erase(history.begin(), history.begin() + excessCommands);

				// Adjust current index
				currentIndex -= excessCommands;
				if (currentIndex < -1) {
					currentIndex = -1;
				}

				spdlog::info("[CommandHistory] Trimmed {} commands due to size reduction", excessCommands);
			}
		}

		spdlog::debug("[CommandHistory] Set max history size from {} to {} (current: {} commands)",
			oldSize, maxHistorySize, history.size());
	}

	int CommandHistory::GetHistorySize() const {
		return static_cast<int>(history.size());
	}

	bool CommandHistory::CanUndo() const {
		// There is something to undo if currentIndex is in bounds
		return currentIndex >= 0 && currentIndex < static_cast<int>(history.size());
	}

	bool CommandHistory::CanRedo() const {
		// There is something to redo if currentIndex is not at the last command
		return currentIndex + 1 < static_cast<int>(history.size());
	}

	std::string CommandHistory::GetUndoDescription() const {
		if (CanUndo()) {
			return "Undo " + history[currentIndex]->GetDescription();
		}
		return "";
	}

	std::string CommandHistory::GetRedoDescription() const {
		if (CanRedo()) {
			return "Redo " + history[currentIndex + 1]->GetDescription();
		}
		return "";
	}

	void CommandHistory::TrimHistory() {
		if (static_cast<int>(history.size()) > maxHistorySize) {
			int excessCount = static_cast<int>(history.size()) - maxHistorySize;
			history.erase(history.begin(), history.begin() + excessCount);
			currentIndex -= excessCount;

			// Ensure currentIndex is still valid
			if (currentIndex < -1) {
				currentIndex = -1;
			}

			spdlog::debug("[CommandHistory] Trimmed {} commands from history", excessCount);
		}
	}

} // namespace WanderSpire