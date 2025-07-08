#pragma once

#include <string>
#include <filesystem>
#include <functional>
#include <system_error>

namespace WanderSpire {

	/// Improved AssetManager with better error handling and consistency
	class AssetManager {
	public:
		/// Result structure for file operations
		struct LoadResult {
			std::string content;
			std::string error;
			bool success = false;

			LoadResult() = default;
			LoadResult(std::string data) : content(std::move(data)), success(true) {}
			LoadResult(std::string err, bool) : error(std::move(err)), success(false) {}

			operator bool() const { return success; }
		};

		/// Callback for async operations
		using AsyncCallback = std::function<void(LoadResult)>;

		/// Set the root folder for all assets
		static void Initialize(const std::filesystem::path& assetsRoot);

		/// Synchronously load a text file (blocking)
		/// Returns LoadResult with file contents or error message
		static LoadResult LoadTextFile(const std::string& relativePath);

		/// Asynchronously load a text file
		/// Callback will be invoked on the main thread with the result
		static void LoadTextFileAsync(const std::string& relativePath, AsyncCallback callback);

		/// Check if a file exists relative to assets root
		static bool FileExists(const std::string& relativePath);

		/// Get file size without loading content (returns 0 on error)
		static size_t GetFileSize(const std::string& relativePath);

		/// Get file modification time (returns epoch time on error)
		static std::filesystem::file_time_type GetFileModTime(const std::string& relativePath);

		/// Resolve relative path to absolute path
		static std::filesystem::path ResolvePath(const std::string& relativePath);

		/// Get the currently configured assets root
		static const std::filesystem::path& GetAssetsRoot() { return m_AssetsRoot; }

	private:
		static std::filesystem::path m_AssetsRoot;

		/// Internal helper for safer file loading
		static LoadResult LoadTextFileInternal(const std::filesystem::path& fullPath);
	};

} // namespace WanderSpire