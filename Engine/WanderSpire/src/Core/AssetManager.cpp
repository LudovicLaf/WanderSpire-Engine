#include "WanderSpire/Core/AssetManager.h"
#include "WanderSpire/Core/AssetLoader.h"

#include <fstream>
#include <sstream>
#include <iostream>
#include <spdlog/spdlog.h>

namespace WanderSpire {

	std::filesystem::path AssetManager::m_AssetsRoot;

	void AssetManager::Initialize(const std::filesystem::path& assetsRoot)
	{
		m_AssetsRoot = assetsRoot;

		// Validate assets root exists
		if (!std::filesystem::exists(m_AssetsRoot)) {
			spdlog::warn("[AssetManager] Assets root does not exist, creating: {}",
				std::filesystem::absolute(m_AssetsRoot).string());
			try {
				std::filesystem::create_directories(m_AssetsRoot);
			}
			catch (const std::exception& e) {
				spdlog::error("[AssetManager] Failed to create assets root: {}", e.what());
			}
		}

		spdlog::info("[AssetManager] Initialized with root: {}",
			std::filesystem::absolute(m_AssetsRoot).string());
	}

	AssetManager::LoadResult AssetManager::LoadTextFile(const std::string& relativePath)
	{
		std::filesystem::path fullPath = ResolvePath(relativePath);
		return LoadTextFileInternal(fullPath);
	}

	void AssetManager::LoadTextFileAsync(
		const std::string& relativePath,
		AsyncCallback callback)
	{
		if (!callback) {
			spdlog::error("[AssetManager] Null callback for async load: {}", relativePath);
			return;
		}

		std::filesystem::path fullPath = ResolvePath(relativePath);

		// Enqueue disk I/O on the worker thread
		AssetLoader::Get().Enqueue([fullPath, cb = std::move(callback)]() mutable {
			LoadResult result = LoadTextFileInternal(fullPath);

			// Marshal the callback back onto the main thread
			AssetLoader::Get().EnqueueMainThread([cb = std::move(cb), result = std::move(result)]() mutable {
				cb(std::move(result));
				});
			});
	}

	bool AssetManager::FileExists(const std::string& relativePath)
	{
		try {
			std::filesystem::path fullPath = ResolvePath(relativePath);
			return std::filesystem::exists(fullPath) && std::filesystem::is_regular_file(fullPath);
		}
		catch (const std::exception& e) {
			spdlog::debug("[AssetManager] FileExists check failed for '{}': {}", relativePath, e.what());
			return false;
		}
	}

	size_t AssetManager::GetFileSize(const std::string& relativePath)
	{
		try {
			std::filesystem::path fullPath = ResolvePath(relativePath);
			if (!std::filesystem::exists(fullPath)) return 0;
			return std::filesystem::file_size(fullPath);
		}
		catch (const std::exception& e) {
			spdlog::debug("[AssetManager] GetFileSize failed for '{}': {}", relativePath, e.what());
			return 0;
		}
	}

	std::filesystem::file_time_type AssetManager::GetFileModTime(const std::string& relativePath)
	{
		try {
			std::filesystem::path fullPath = ResolvePath(relativePath);
			if (!std::filesystem::exists(fullPath)) return {};
			return std::filesystem::last_write_time(fullPath);
		}
		catch (const std::exception& e) {
			spdlog::debug("[AssetManager] GetFileModTime failed for '{}': {}", relativePath, e.what());
			return {};
		}
	}

	std::filesystem::path AssetManager::ResolvePath(const std::string& relativePath)
	{
		return m_AssetsRoot / relativePath;
	}

	AssetManager::LoadResult AssetManager::LoadTextFileInternal(const std::filesystem::path& fullPath)
	{
		try {
			if (!std::filesystem::exists(fullPath)) {
				return LoadResult("File does not exist: " + fullPath.string(), false);
			}

			if (!std::filesystem::is_regular_file(fullPath)) {
				return LoadResult("Path is not a regular file: " + fullPath.string(), false);
			}

			std::ifstream file(fullPath, std::ios::binary);
			if (!file.is_open()) {
				return LoadResult("Failed to open file: " + fullPath.string(), false);
			}

			// Get file size for efficient allocation
			file.seekg(0, std::ios::end);
			size_t size = file.tellg();
			file.seekg(0, std::ios::beg);

			std::string content;
			content.reserve(size);

			std::stringstream buffer;
			buffer << file.rdbuf();
			content = buffer.str();

			if (file.bad()) {
				return LoadResult("Error reading file: " + fullPath.string(), false);
			}

			return LoadResult(std::move(content));

		}
		catch (const std::exception& e) {
			return LoadResult("Exception loading file '" + fullPath.string() + "': " + e.what(), false);
		}
	}

}