#pragma once

#include <unordered_map>
#include <string>
#include <nlohmann/json.hpp>

namespace WanderSpire {

	struct AnimationClipsComponent {
		struct Clip {
			int   startFrame = 0;
			int   frameCount = 1;
			float frameDuration = 0.1f;
			bool  loop = true;
		};

		// Map from arbitrary string (clip name) → Clip data
		std::unordered_map<std::string, Clip> clips;

		/// Load all clips from a JSON object whose keys are clip-names (strings).
		void LoadFromJson(const nlohmann::json& j)
		{
			clips.clear();
			for (auto const& [name, obj] : j.items())
			{
				Clip c;
				c.startFrame = obj.value("start", c.startFrame);
				c.frameCount = obj.value("count", c.frameCount);
				c.frameDuration = obj.value("duration", c.frameDuration);
				c.loop = obj.value("loop", c.loop);
				clips[name] = c;
			}
		}

		/// Serialize back into JSON with each key = clip-name
		nlohmann::json ToJson() const
		{
			nlohmann::json j;
			for (auto const& [name, c] : clips)
			{
				j[name] = {
					{ "start",    c.startFrame    },
					{ "count",    c.frameCount    },
					{ "duration", c.frameDuration },
					{ "loop",     c.loop          }
				};
			}
			return j;
		}
	};

} // namespace WanderSpire

REFLECT_TYPE(WanderSpire::AnimationClipsComponent)
