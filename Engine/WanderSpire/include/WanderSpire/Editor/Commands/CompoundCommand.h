#pragma once
#include "WanderSpire/Editor/ICommand.h"
#include <vector>
#include <memory>
#include <string>

namespace WanderSpire {

	class CompoundCommand : public ICommand {
	public:
		CompoundCommand(const std::string& description);
		void AddCommand(std::unique_ptr<ICommand> command);
		void Execute() override;
		void Undo() override;
		std::string GetDescription() const override;
	private:
		std::string description;
		std::vector<std::unique_ptr<ICommand>> commands;
	};

} // namespace WanderSpire
