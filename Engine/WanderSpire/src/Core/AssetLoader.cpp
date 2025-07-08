#include "WanderSpire/Core/AssetLoader.h"
#include <spdlog/spdlog.h>

namespace WanderSpire {

	AssetLoader& AssetLoader::Get() {
		static AssetLoader inst;
		return inst;
	}

	AssetLoader::AssetLoader() {
		_worker = std::thread(&AssetLoader::LoaderLoop, this);
	}

	AssetLoader::~AssetLoader() {
		{
			std::lock_guard lk(_workMtx);
			_running = false;
		}
		_workCv.notify_all();
		if (_worker.joinable()) _worker.join();
	}

	void AssetLoader::Enqueue(std::function<void()> work) {
		{
			std::lock_guard lk(_workMtx);
			_workQueue.push(std::move(work));
		}
		_workCv.notify_one();
	}

	void AssetLoader::EnqueueMainThread(std::function<void()> cb) {
		std::lock_guard lk(_mainMtx);
		_mainQueue.push(std::move(cb));
	}

	void AssetLoader::LoaderLoop() {
		while (true) {
			std::function<void()> task;
			{
				std::unique_lock lk(_workMtx);
				_workCv.wait(lk, [&] { return !_workQueue.empty() || !_running; });
				if (!_running && _workQueue.empty()) break;
				task = std::move(_workQueue.front());
				_workQueue.pop();
			}
			try {
				task();
			}
			catch (const std::exception& e) {
				spdlog::error("[AssetLoader] background task threw: {}", e.what());
			}
		}
	}

	void AssetLoader::UpdateMainThread() {
		std::queue<std::function<void()>> q;
		{
			std::lock_guard lk(_mainMtx);
			std::swap(q, _mainQueue);
		}
		while (!q.empty()) {
			try {
				q.front()();
			}
			catch (const std::exception& e) {
				spdlog::error("[AssetLoader] main-thread task threw: {}", e.what());
			}
			q.pop();
		}
	}

}
