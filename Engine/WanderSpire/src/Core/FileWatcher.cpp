#include "WanderSpire/Core/FileWatcher.h"
#include <spdlog/spdlog.h>

namespace WanderSpire {

	FileWatcher& FileWatcher::Get() {
		static FileWatcher inst;
		return inst;
	}

	void FileWatcher::WatchFile(const std::filesystem::path& path, std::function<void()> callback) {
		std::filesystem::file_time_type t{};
		if (std::filesystem::exists(path))
			t = std::filesystem::last_write_time(path);
		_files.push_back({ path, t, std::move(callback) });
	}

	void FileWatcher::WatchDirectory(const std::filesystem::path& dir,
		const std::vector<std::string>& extensions,
		std::function<void(const std::filesystem::path&)> callback)
	{
		DirWatch dw;
		dw.dir = dir;
		dw.exts = extensions;
		dw.callback = std::move(callback);

		// seed existing files
		if (std::filesystem::exists(dir)) {
			for (auto& e : std::filesystem::directory_iterator(dir)) {
				if (!e.is_regular_file()) continue;
				auto ext = e.path().extension().string();
				for (auto const& want : dw.exts) {
					if (ext == want) {
						dw.times[e.path().string()] = std::filesystem::last_write_time(e);
						break;
					}
				}
			}
		}

		_dirs.push_back(std::move(dw));
	}

	void FileWatcher::Update() {
		// check individual files
		for (auto& fw : _files) {
			if (!std::filesystem::exists(fw.path)) continue;
			auto newTime = std::filesystem::last_write_time(fw.path);
			if (newTime != fw.lastWrite) {
				fw.lastWrite = newTime;
				spdlog::info("[FileWatcher] file changed: {}", fw.path.string());
				fw.callback();
			}
		}

		// check directories
		for (auto& dw : _dirs) {
			if (!std::filesystem::exists(dw.dir)) continue;
			for (auto& e : std::filesystem::directory_iterator(dw.dir)) {
				if (!e.is_regular_file()) continue;
				auto p = e.path();
				auto ext = p.extension().string();
				bool matches = false;
				for (auto const& want : dw.exts) {
					if (ext == want) { matches = true; break; }
				}
				if (!matches) continue;

				auto key = p.string();
				auto newTime = std::filesystem::last_write_time(p);
				auto it = dw.times.find(key);

				if (it == dw.times.end()) {
					// new file!
					dw.times[key] = newTime;
					spdlog::info("[FileWatcher] new file: {}", key);
					dw.callback(p);
				}
				else if (newTime != it->second) {
					// modified
					it->second = newTime;
					spdlog::info("[FileWatcher] dir file changed: {}", key);
					dw.callback(p);
				}
			}
		}
	}

}
