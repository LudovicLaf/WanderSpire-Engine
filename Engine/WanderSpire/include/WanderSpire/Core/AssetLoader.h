#pragma once

#include <functional>
#include <thread>
#include <mutex>
#include <queue>
#include <condition_variable>

namespace WanderSpire {

	class AssetLoader {
	public:
		/// Get the singleton loader.
		static AssetLoader& Get();

		/// Push work onto the loader thread.
		/// The function runs on the worker thread.
		void Enqueue(std::function<void()> work);

		/// Schedule a callback to run on the main thread. 
		/// Call in your main loop: UpdateMainThread().
		void EnqueueMainThread(std::function<void()> cb);

		/// Must be called each frame on the main thread to flush main-thread callbacks.
		void UpdateMainThread();

	private:
		AssetLoader();
		~AssetLoader();

		void LoaderLoop();

		std::thread                           _worker;
		bool                                  _running = true;

		std::mutex                            _workMtx;
		std::condition_variable               _workCv;
		std::queue<std::function<void()>>     _workQueue;

		std::mutex                            _mainMtx;
		std::queue<std::function<void()>>     _mainQueue;
	};

}
