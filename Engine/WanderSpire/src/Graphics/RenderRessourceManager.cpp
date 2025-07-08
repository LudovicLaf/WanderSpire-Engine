#include "WanderSpire/Graphics/RenderResourceManager.h"
#include "WanderSpire/Core/AssetManager.h"
#include "WanderSpire/Core/AssetLoader.h"
#include "WanderSpire/External/stb_image.h"
#define STB_IMAGE_WRITE_IMPLEMENTATION
#include "WanderSpire/External/stb_image_write.h"

#include <nlohmann/json.hpp>
#include <filesystem>
#include <spdlog/spdlog.h>
#include <cmath>
#include <fstream>
#include <cstring>

namespace WanderSpire {
	namespace {
		int NextPow2(int v) {
			v--; v |= v >> 1; v |= v >> 2; v |= v >> 4; v |= v >> 8; v |= v >> 16;
			v++;
			return v > 0 ? v : 1;
		}

		bool IsSupportedImageExtension(const std::string& ext) {
			std::string lowerExt = ext;
			std::transform(lowerExt.begin(), lowerExt.end(), lowerExt.begin(), ::tolower);
			return lowerExt == ".png" || lowerExt == ".jpg" || lowerExt == ".jpeg";
		}
	}

	RenderResourceManager& RenderResourceManager::Get() {
		static RenderResourceManager instance;
		return instance;
	}

	void RenderResourceManager::Init(GLuint vao, GLuint ebo) {
		m_QuadVAO = vao;
		m_QuadEBO = ebo;
	}

	void RenderResourceManager::OnContextBound()
	{
		/* Example: re-query GL limits, re-create the quad VAO/EBO, etc. */
		if (m_QuadVAO == 0 || m_QuadEBO == 0)
		{
			GLuint vao{}, ebo{};
			glGenVertexArrays(1, &vao);
			glGenBuffers(1, &ebo);
			Init(vao, ebo);            // reuse your existing helper
		}

		GLint maxTex = 0;
		glGetIntegerv(GL_MAX_TEXTURE_SIZE, &maxTex);
		spdlog::info("[RRM] New GL context bound (max tex = {})", maxTex);
	}


	// --- Shader registration with async compile ---
	void RenderResourceManager::RegisterShader(
		const std::string& name,
		const std::string& vsPath,
		const std::string& fsPath)
	{
		// 1) Insert placeholder so GetShader never returns null
		m_Shaders[name] = std::make_unique<Shader>();

		// 2) Enqueue worker task: load text from disk
		AssetLoader::Get().Enqueue([this, name, vsPath, fsPath]() {
			auto vsResult = AssetManager::LoadTextFile(vsPath);
			auto fsResult = AssetManager::LoadTextFile(fsPath);

			// 3) Enqueue main-thread compile/link
			AssetLoader::Get().EnqueueMainThread([this, name, vsResult, fsResult]() mutable {
				if (!vsResult) {
					spdlog::error("[RenderResourceManager] Failed to load vertex shader '{}': {}", name, vsResult.error);
					return;
				}
				if (!fsResult) {
					spdlog::error("[RenderResourceManager] Failed to load fragment shader '{}': {}", name, fsResult.error);
					return;
				}

				auto shader = std::make_unique<Shader>();
				shader->CompileFromSource(vsResult.content, fsResult.content);
				m_Shaders[name] = std::move(shader);
				spdlog::info("[HotReload] Shader '{}' recompiled", name);
				});
			});
	}

	Shader* RenderResourceManager::GetShader(const std::string& name) {
		auto it = m_Shaders.find(name);
		return it != m_Shaders.end() ? it->second.get() : nullptr;
	}

	/*──────────────────────── textures ────────────────────────*/
	void RenderResourceManager::RegisterTexture(const std::string& name,
		const std::string& texturePath)
	{
		/* ---------------------------------------------------------------------
		   STEP 1 – get or create the *persistent* holder that all
					SpriteAnimationComponents will keep pointing at.
		   -------------------------------------------------------------------*/
		std::shared_ptr<Texture> holder;
		auto it = m_Textures.find(name);
		if (it == m_Textures.end())
		{
			holder = std::make_shared<Texture>();        // 1×1 white stub
			m_Textures[name] = holder;
		}
		else
		{
			holder = it->second;
			if (!holder) {
				holder = std::make_shared<Texture>();    // safety net
				it->second = holder;
			}
		}

		/* ---------------------------------------------------------------------
		   STEP 2 – disk‑I/O and PNG decode on the worker thread
		   -------------------------------------------------------------------*/
		AssetLoader::Get().Enqueue([holder, name, texturePath]()
			{
				namespace fs = std::filesystem;
				auto full = AssetManager::GetAssetsRoot() / texturePath;

				int w, h, ch;
				stbi_set_flip_vertically_on_load(false);
				unsigned char* pixels = stbi_load(full.string().c_str(),
					&w, &h, &ch, STBI_rgb_alpha);
				if (!pixels) {
					spdlog::error("[AsyncTex] fail to load '{}'", full.string());
					return;
				}
				const size_t bytes = size_t(w) * size_t(h) * 4;
				std::vector<unsigned char> data(pixels, pixels + bytes);
				stbi_image_free(pixels);

				/* -----------------------------------------------------------------
				   STEP 3 – GPU upload *on the main thread* into the SAME object
				   ----------------------------------------------------------------*/
				AssetLoader::Get().EnqueueMainThread(
					[holder, name, w, h, data = std::move(data)]() mutable
					{
						holder->UploadFromData(data.data(), w, h);
						spdlog::info("[HotReload] Texture '{}' uploaded ({}×{})",
							name, w, h);
					});
			});
	}

	/*──────────────────────── atlases ─────────────────────────*/
	void RenderResourceManager::RegisterAtlas(const std::string& name,
		const std::string& atlasImagePath,
		const std::string& mappingJsonPath)
	{
		auto it = m_Atlases.find(name);
		if (it == m_Atlases.end())
		{
			auto atlas = std::make_unique<TextureAtlas>();
			atlas->Load(atlasImagePath, mappingJsonPath);
			m_Atlases[name] = std::move(atlas);
		}
		else
		{
			/* keep the unique_ptr stable – just refresh its contents */
			it->second->Load(atlasImagePath, mappingJsonPath);
		}
	}

	std::shared_ptr<Texture> RenderResourceManager::GetTexture(
		const std::string& name)
	{
		auto it = m_Textures.find(name);
		return it != m_Textures.end() ? it->second : nullptr;
	}

	TextureAtlas* RenderResourceManager::GetAtlas(const std::string& name) {
		auto it = m_Atlases.find(name);
		return it != m_Atlases.end() ? it->second.get() : nullptr;
	}

	size_t RenderResourceManager::GetAtlasCount() const {
		return m_Atlases.size();
	}

	/*──────────────────────── NEW: Spritesheet Registration ─────────────────────────*/
	void RenderResourceManager::RegisterSpritesheets(const std::string& spriteSheetsRoot) {
		namespace fs = std::filesystem;
		fs::path baseDir = AssetManager::GetAssetsRoot() / spriteSheetsRoot;

		if (!fs::exists(baseDir) || !fs::is_directory(baseDir)) {
			spdlog::warn("[RenderResourceManager] SpriteSheets directory '{}' not found", baseDir.string());
			return;
		}

		size_t registeredCount = 0;

		// Recursively scan for image files
		for (auto const& entry : fs::recursive_directory_iterator(baseDir)) {
			if (!entry.is_regular_file()) continue;

			auto ext = entry.path().extension().string();
			if (!IsSupportedImageExtension(ext)) continue;

			// Calculate relative path from SpriteSheets root
			auto relativePath = fs::relative(entry.path(), baseDir);
			std::string key = relativePath.generic_string(); // Use forward slashes

			// Register the spritesheet with its relative path as key
			std::string fullRelativePath = spriteSheetsRoot + "/" + key;
			RegisterTexture(key, fullRelativePath);
			registeredCount++;

			spdlog::debug("[RenderResourceManager] Registered spritesheet '{}' -> '{}'",
				key, fullRelativePath);
		}

		spdlog::info("[RenderResourceManager] Auto-registered {} spritesheets from '{}'",
			registeredCount, baseDir.string());
	}

	// --- Atlas generation (unchanged) ---
	void RenderResourceManager::GenerateAtlases(const std::string& texturesSubfolder) {
		namespace fs = std::filesystem;
		fs::path baseDir = AssetManager::GetAssetsRoot() / texturesSubfolder;
		if (!fs::exists(baseDir) || !fs::is_directory(baseDir)) {
			spdlog::warn("[AtlasGen] '{}' not a directory", baseDir.string());
			return;
		}

		GLint glMax = 0;
		glGetIntegerv(GL_MAX_TEXTURE_SIZE, &glMax);
		int maxSize = glMax;

		for (auto const& dirEntry : fs::directory_iterator(baseDir)) {
			if (!dirEntry.is_directory()) continue;
			std::string atlasName = dirEntry.path().filename().string();

			struct Img { std::string name; int w, h; std::vector<unsigned char> data; };
			std::vector<Img> images;
			uint64_t totalArea = 0;

			// Load individual images
			for (auto const& fileEntry : fs::directory_iterator(dirEntry.path())) {
				auto ext = fileEntry.path().extension().string();
				std::transform(ext.begin(), ext.end(), ext.begin(), ::tolower);
				if (ext != ".png" && ext != ".jpg" && ext != ".jpeg") continue;

				int w, h, ch;
				unsigned char* pix = stbi_load(
					fileEntry.path().string().c_str(), &w, &h, &ch, 4
				);
				if (!pix) {
					spdlog::error("[AtlasGen] fail to load '{}'", fileEntry.path().string());
					continue;
				}
				Img img{ fileEntry.path().stem().string(), w, h, {} };
				img.data.assign(pix, pix + w * h * 4);
				stbi_image_free(pix);

				images.push_back(std::move(img));
				totalArea += uint64_t(w) * uint64_t(h);
			}

			if (images.empty()) {
				spdlog::warn("[AtlasGen] '{}' empty", dirEntry.path().string());
				continue;
			}

			// Determine atlas size
			float approx = std::sqrt((double)totalArea);
			int atlasW = NextPow2(std::min(approx, (float)maxSize));
			atlasW = std::max(atlasW, images[0].w);

			// Pack images in simple shelf algorithm
			struct Rect { std::string name; int x, y, w, h; };
			std::vector<Rect> rects;
			int x = 0, y = 0, shelf = 0;
			for (auto const& img : images) {
				if (x + img.w > atlasW) {
					x = 0;
					y += shelf;
					shelf = 0;
				}
				rects.push_back({ img.name, x, y, img.w, img.h });
				x += img.w;
				shelf = std::max(shelf, img.h);
			}
			int atlasH = NextPow2(y + shelf);

			// Build atlas image buffer
			std::vector<unsigned char> buf(atlasW * atlasH * 4, 0);
			for (size_t i = 0; i < rects.size(); ++i) {
				auto const& r = rects[i];
				auto const& img = images[i];
				for (int row = 0; row < r.h; ++row) {
					unsigned char* dst = &buf[((r.y + row) * atlasW + r.x) * 4];
					const unsigned char* src = img.data.data() + row * img.w * 4;
					std::memcpy(dst, src, img.w * 4);
				}
			}

			// Write out PNG
			fs::path atlasPng = baseDir / (atlasName + "_atlas.png");
			stbi_write_png(
				atlasPng.string().c_str(),
				atlasW, atlasH, 4, buf.data(),
				atlasW * 4
			);

			// Build JSON mapping
			nlohmann::json j;
			j["meta"] = { {"width", atlasW}, {"height", atlasH} };
			for (auto const& r : rects) {
				j["frames"][r.name] = {
					{"x", r.x}, {"y", r.y},
					{"w", r.w}, {"h", r.h}
				};
			}

			// Write out JSON
			fs::path atlasJson = baseDir / (atlasName + "_atlas.json");
			fs::create_directories(atlasJson.parent_path());
			std::ofstream of(atlasJson.string(), std::ios::trunc);
			of << j.dump(2);
			of.close();
			spdlog::info("[AtlasGen] Wrote mapping {}", atlasJson.string());

			// **Always** register the atlas for use (hot-reload will catch updates)
			RegisterAtlas(
				atlasName,
				texturesSubfolder + "/" + atlasPng.filename().string(),
				texturesSubfolder + "/" + atlasJson.filename().string()
			);
			spdlog::info("[AtlasGen] Registered atlas '{}'", atlasName);
		}
	}

}