#include "WanderSpire/Editor/Commands/CompoundCommand.h"

namespace WanderSpire {

	CompoundCommand::CompoundCommand(const std::string& description) : description(description) {}

	void CompoundCommand::AddCommand(std::unique_ptr<ICommand> command) {
		if (command) commands.push_back(std::move(command));
	}

	void CompoundCommand::Execute() {
		for (auto& command : commands) command->Execute();
	}

	void CompoundCommand::Undo() {
		for (auto it = commands.rbegin(); it != commands.rend(); ++it)
			(*it)->Undo();
	}

	std::string CompoundCommand::GetDescription() const { return description; }

} // namespace WanderSpire
