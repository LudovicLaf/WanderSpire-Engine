#include "WanderSpire/Input/InputManager.h"

#include <nlohmann/json.hpp>
#include <fstream>

using json = nlohmann::json;
using namespace WanderSpire;

/* ── static storage ───────────────────────────────────────────────────────*/
std::unordered_map<std::string, SDL_Keycode> InputManager::s_keyBind;
std::unordered_map<std::string, MouseButton> InputManager::s_mouseBind;

std::unordered_set<SDL_Keycode> InputManager::s_keysDown;
std::unordered_set<SDL_Keycode> InputManager::s_keysPressed;
std::unordered_set<SDL_Keycode> InputManager::s_keysReleased;

std::unordered_set<MouseButton> InputManager::s_mouseDown;
std::unordered_set<MouseButton> InputManager::s_mousePressed;
std::unordered_set<MouseButton> InputManager::s_mouseReleased;

MouseState InputManager::s_mouse = { 0, 0 };
int        InputManager::gScrollDelta = 0;

/* ── helpers ──────────────────────────────────────────────────────────────*/
MouseButton InputManager::toMouseButton(uint8_t b)
{
	switch (b)
	{
	case SDL_BUTTON_LEFT:   return MouseButton::Left;
	case SDL_BUTTON_MIDDLE: return MouseButton::Middle;
	case SDL_BUTTON_RIGHT:  return MouseButton::Right;
	case SDL_BUTTON_X1:     return MouseButton::X1;
	case SDL_BUTTON_X2:     return MouseButton::X2;
	default:                return MouseButton::Left;
	}
}

/* ── life-cycle ───────────────────────────────────────────────────────────*/
void InputManager::Initialize()
{
	s_keyBind.clear();
	s_mouseBind.clear();
	s_keysDown.clear();   s_keysPressed.clear();   s_keysReleased.clear();
	s_mouseDown.clear();  s_mousePressed.clear();  s_mouseReleased.clear();
	gScrollDelta = 0;
}

void InputManager::Update()
{
	s_keysPressed.clear();
	s_keysReleased.clear();
	s_mousePressed.clear();
	s_mouseReleased.clear();
	gScrollDelta = 0;
}

/* ── bindings ─────────────────────────────────────────────────────────────*/
void InputManager::BindAction(const std::string& name, SDL_Keycode key)
{
	s_keyBind[name] = key;
}
void InputManager::BindMouseAction(const std::string& name, MouseButton btn)
{
	s_mouseBind[name] = btn;
}

void InputManager::SaveBindingsToFile(const std::string& file)
{
	json j;
	for (auto& [n, k] : s_keyBind)   j["keys"][n] = k;
	for (auto& [n, b] : s_mouseBind) j["mouse"][n] = static_cast<int>(b);
	std::ofstream(file) << j.dump(2);
}

/* ── event pump ───────────────────────────────────────────────────────────*/
void InputManager::HandleEvent(const SDL_Event& e)
{
	switch (e.type)
	{
		/* -------- keyboard -------- */
	case SDL_EVENT_KEY_DOWN:
		if (e.key.repeat == 0)
		{
			s_keysDown.insert(e.key.key);   /* SDL3: .key & .scancode */
			s_keysPressed.insert(e.key.key);
		}
		break;

	case SDL_EVENT_KEY_UP:
		s_keysDown.erase(e.key.key);
		s_keysReleased.insert(e.key.key);
		break;

		/* -------- mouse buttons --- */
	case SDL_EVENT_MOUSE_BUTTON_DOWN:
	{
		MouseButton btn = toMouseButton(e.button.button);
		s_mouseDown.insert(btn);
		s_mousePressed.insert(btn);
		break;
	}
	case SDL_EVENT_MOUSE_BUTTON_UP:
	{
		MouseButton btn = toMouseButton(e.button.button);
		s_mouseDown.erase(btn);
		s_mouseReleased.insert(btn);
		break;
	}

	/* -------- wheel ----------- */
	case SDL_EVENT_MOUSE_WHEEL:
		gScrollDelta += static_cast<int>(e.wheel.y);   /* +ve up / –ve down */
		break;

		/* -------- motion ---------- */
	case SDL_EVENT_MOUSE_MOTION:
		s_mouse.x = e.motion.x;
		s_mouse.y = e.motion.y;
		break;

	default: break;
	}
}

/* ── queries – keyboard ───────────────────────────────────────────────────*/
bool InputManager::IsActionHeld(const std::string& n)
{
	auto it = s_keyBind.find(n);
	return (it != s_keyBind.end()) && s_keysDown.contains(it->second);
}
bool InputManager::IsActionPressed(const std::string& n)
{
	auto it = s_keyBind.find(n);
	return (it != s_keyBind.end()) && s_keysPressed.contains(it->second);
}
bool InputManager::IsActionReleased(const std::string& n)
{
	auto it = s_keyBind.find(n);
	return (it != s_keyBind.end()) && s_keysReleased.contains(it->second);
}

/* ── queries – mouse ──────────────────────────────────────────────────────*/
bool InputManager::IsMouseActionHeld(const std::string& n)
{
	auto it = s_mouseBind.find(n);
	return (it != s_mouseBind.end()) && s_mouseDown.contains(it->second);
}
bool InputManager::IsMouseActionPressed(const std::string& n)
{
	auto it = s_mouseBind.find(n);
	return (it != s_mouseBind.end()) && s_mousePressed.contains(it->second);
}
bool InputManager::IsMouseActionReleased(const std::string& n)
{
	auto it = s_mouseBind.find(n);
	return (it != s_mouseBind.end()) && s_mouseReleased.contains(it->second);
}

/* ── misc -----------------------------------------------------------------*/
MouseState& InputManager::GetMouseState() { return s_mouse; }
int         InputManager::GetScrollDelta() { return gScrollDelta; }
