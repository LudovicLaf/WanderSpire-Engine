#pragma once
#include <functional>
#include <typeindex>
#include <unordered_map>
#include <vector>
#include <mutex>

namespace WanderSpire {

	/** Simple, header‑only publish / subscribe bus.
	 *
	 *  * Any POD or struct can be an *event* type – no base‑class needed.
	 *  * Listeners may subscribe on any thread; `Publish` is thread‑safe.
	 *  * A `Subscription` RAII token unsubscribes automatically on destruction.
	 */
	class EventBus {
	public:
		/** Access the global bus (one per process). */
		static EventBus& Get();

		/** RAII handle for a live subscription. */
		class Subscription {
			friend class EventBus;
		public:
			Subscription() = default;
			Subscription(const Subscription&) = delete;
			Subscription& operator=(const Subscription&) = delete;

			Subscription(Subscription&& other) noexcept
				: _bus(other._bus), _type(other._type), _id(other._id)
			{
				other._id = 0;
			}

			Subscription& operator=(Subscription&& rhs) noexcept;

			~Subscription();

		private:
			Subscription(EventBus* bus,
				std::type_index type,
				std::size_t     id)
				: _bus(bus), _type(type), _id(id) {
			}

			EventBus* _bus = nullptr;
			std::type_index _type{ typeid(void) };
			std::size_t    _id = 0;
		};

		/** Subscribe – returns a token that removes the callback on destruction. */
		template<typename E>
		Subscription Subscribe(std::function<void(const E&)> cb)
		{
			std::lock_guard lk(_mtx);
			auto& vec = _slots[typeid(E)];
			std::size_t id = ++_nextId;
			vec.push_back({ id,
							[fn = std::move(cb)](const void* ev)
								{ fn(*static_cast<const E*>(ev)); } });
			return Subscription(this, typeid(E), id);
		}

		/** Fire an event – copies the payload once, then delivers to all slots. */
		template<typename E>
		void Publish(const E& ev)
		{
			std::vector<Slot> local;
			{
				std::lock_guard lk(_mtx);
				auto it = _slots.find(typeid(E));
				if (it == _slots.end()) return;
				local = it->second;          // copy so callbacks may mutate bus
			}
			for (auto& s : local) s.fn(&ev);
		}

	private:
		struct Slot {
			std::size_t id;
			std::function<void(const void*)> fn;
		};

		void _unsubscribe(std::type_index type, std::size_t id);

		std::mutex _mtx;
		std::unordered_map<std::type_index, std::vector<Slot>> _slots;
		std::size_t _nextId = 0;
	};

} // namespace WanderSpire
