#pragma once
#include "WanderSpire/Editor/ICommand.h"
#include <entt/entt.hpp>
#include <vector>

namespace WanderSpire {

	class SelectionCommand : public ICommand {
	public:
		SelectionCommand(entt::registry& registry, const std::vector<entt::entity>& newSelection);
		void Execute() override;
		void Undo() override;
		std::string GetDescription() const override;
	private:
		entt::registry* registry;
		std::vector<entt::entity> oldSelection, newSelection;
	};

} // namespace WanderSpire
