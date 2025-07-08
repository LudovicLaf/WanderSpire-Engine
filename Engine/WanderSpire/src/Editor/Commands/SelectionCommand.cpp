#include "WanderSpire/Editor/Commands/SelectionCommands.h"
#include "WanderSpire/Editor/SelectionManager.h"

namespace WanderSpire {

	SelectionCommand::SelectionCommand(entt::registry& registry, const std::vector<entt::entity>& newSelection)
		: registry(&registry), newSelection(newSelection) {
		const auto& currentSelected = SelectionManager::GetInstance().GetSelectedEntities();
		oldSelection.assign(currentSelected.begin(), currentSelected.end());
	}

	std::string SelectionCommand::GetDescription() const {
		if (newSelection.size() == 1)
			return "Select 1 entity";
		else if (newSelection.empty())
			return "Deselect all";
		else
			return "Select " + std::to_string(newSelection.size()) + " entities";
	}

	void SelectionCommand::Execute() {
		SelectionManager::GetInstance().SetSelection(*registry, newSelection);
	}

	void SelectionCommand::Undo() {
		SelectionManager::GetInstance().SetSelection(*registry, oldSelection);
	}

} // namespace WanderSpire
