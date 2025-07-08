#pragma once
#include <vector>
#include <memory>
#include <string>
#include "ICommand.h"

namespace WanderSpire {

	class CommandHistory {
	public:
		void ExecuteCommand(std::unique_ptr<ICommand> command);
		bool CanUndo() const;
		bool CanRedo() const;

		void Undo();
		void Redo();
		void Clear();

		std::string GetUndoDescription() const;
		std::string GetRedoDescription() const;

		void SetMaxHistorySize(int size);
		int GetHistorySize() const;

	private:
		std::vector<std::unique_ptr<ICommand>> history;
		int currentIndex = -1;
		int maxHistorySize = 100;

		void TrimHistory();
	};

} // namespace WanderSpire
