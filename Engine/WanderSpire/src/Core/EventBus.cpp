#include "WanderSpire/Core/EventBus.h"

namespace WanderSpire {

	EventBus& EventBus::Get()
	{
		static EventBus inst;
		return inst;
	}

	// ── Subscription helpers ────────────────────────────────────────────────────
	EventBus::Subscription& EventBus::Subscription::operator=(
		Subscription&& rhs) noexcept
	{
		if (this == &rhs) return *this;
		// clean existing
		if (_id) _bus->_unsubscribe(_type, _id);

		_bus = rhs._bus;
		_type = rhs._type;
		_id = rhs._id;
		rhs._id = 0;
		return *this;
	}

	EventBus::Subscription::~Subscription()
	{
		if (_id) _bus->_unsubscribe(_type, _id);
	}

	// ── private helpers ─────────────────────────────────────────────────────────
	void EventBus::_unsubscribe(std::type_index type, std::size_t id)
	{
		std::lock_guard lk(_mtx);
		auto it = _slots.find(type);
		if (it == _slots.end()) return;
		auto& vec = it->second;
		vec.erase(std::remove_if(vec.begin(), vec.end(),
			[id](const Slot& s) { return s.id == id; }),
			vec.end());
	}

} // namespace WanderSpire
