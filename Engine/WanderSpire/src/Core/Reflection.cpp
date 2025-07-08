// WanderSpire/Core/Reflection.cpp
#include "WanderSpire/Core/Reflection.h"
#include <sstream>
#include <cstdio>

namespace Reflect {

	/* ───────────────────── FieldInfo helpers ───────────────────── */

	std::string FieldInfo::getAsString(void* structPtr) const
	{
		char* base = static_cast<char*>(structPtr) + offset;
		std::ostringstream ss;

		switch (type) {
		case FieldType::Float:  ss << *reinterpret_cast<float*>(base); break;
		case FieldType::Int:    ss << *reinterpret_cast<int*>(base);   break;
		case FieldType::Bool:   ss << (*reinterpret_cast<bool*>(base) ? "true" : "false"); break;
		case FieldType::Vec2: {
			auto v = reinterpret_cast<float(*)[2]>(base);
			ss << (*v)[0] << ',' << (*v)[1];
			break;
		}
		case FieldType::String: ss << *reinterpret_cast<std::string*>(base); break;
		}
		return ss.str();
	}

	void FieldInfo::setFromString(void* structPtr, const std::string& s) const
	{
		char* base = static_cast<char*>(structPtr) + offset;

		switch (type) {
		case FieldType::Float:
			*reinterpret_cast<float*>(base) = std::stof(s); break;

		case FieldType::Int:
			*reinterpret_cast<int*>(base) = std::stoi(s);   break;

		case FieldType::Bool: {
			bool v = (s == "1" || s == "true" || s == "True");
			*reinterpret_cast<bool*>(base) = v;
			break;
		}
		case FieldType::Vec2: {
			float x = 0.f, y = 0.f;
			std::sscanf(s.c_str(), "%f,%f", &x, &y);
			auto v = reinterpret_cast<float(*)[2]>(base);
			(*v)[0] = x; (*v)[1] = y;
			break;
		}
		case FieldType::String:
			*reinterpret_cast<std::string*>(base) = s; break;
		}
	}

	/* ─────────────────── TypeInfo::addField ────────────────────── */

	TypeInfo& TypeInfo::addField(const std::string& n,
		FieldType           ft,
		size_t              off,
		float               mn,
		float               mx,
		float               st)
	{
		fields.emplace_back();              // default‑constructed FieldInfo
		FieldInfo& f = fields.back();
		f.name = n;
		f.type = ft;
		f.offset = off;
		f.min = mn;
		f.max = mx;
		f.step = st;
		return *this;
	}

} // namespace Reflect
