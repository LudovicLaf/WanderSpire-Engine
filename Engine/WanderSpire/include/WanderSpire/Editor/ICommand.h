#pragma once
#include <string>

namespace WanderSpire {

	class ICommand {
	public:
		virtual ~ICommand() = default;
		virtual void Execute() = 0;
		virtual void Undo() = 0;
		virtual std::string GetDescription() const = 0;
		virtual bool CanMerge(const ICommand* other) const { return false; }
		virtual void MergeWith(const ICommand* other) {}
	};

} // namespace WanderSpire
