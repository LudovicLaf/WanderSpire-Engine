#pragma once

#include <filesystem>
#include <functional>
#include <unordered_map>
#include <vector>

namespace WanderSpire {

	class FileWatcher {
	public:
		/// Singleton accessor
		static FileWatcher& Get();

		/// Call from your main loop each frame
		void Update();

		/// Watch a single file; callback() fires when its last-write time changes.
		void WatchFile(const std::filesystem::path& path, std::function<void()> callback);

		/// Watch *all* files under `dir` matching one of `extensions` (e.g. {".png",".jpg"}).
		/// On new or modified files, callback(changedFile) is invoked.
		void WatchDirectory(const std::filesystem::path& dir,
			const std::vector<std::string>& extensions,
			std::function<void(const std::filesystem::path&)> callback);

	private:
		FileWatcher() = default;
		~FileWatcher() = default;

		struct FileWatch {
			std::filesystem::path                     path;
			std::filesystem::file_time_type           lastWrite;
			std::function<void()>                     callback;
		};
		struct DirWatch {
			std::filesystem::path                     dir;
			std::vector<std::string>                  exts;
			std::unordered_map<std::string,
				std::filesystem::file_time_type>       times;
			std::function<void(const std::filesystem::path&)> callback;
		};

		std::vector<FileWatch> _files;
		std::vector<DirWatch>  _dirs;
	};

}
