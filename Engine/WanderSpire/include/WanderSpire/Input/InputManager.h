#pragma once
/*─────────────────────────────────────────────────────────────────────────────
  InputManager – unified keyboard / mouse helper for SDL3
─────────────────────────────────────────────────────────────────────────────*/
#include <SDL3/SDL.h>
#include <string>
#include <unordered_map>
#include <unordered_set>

namespace WanderSpire {

	enum class MouseButton { Left, Middle, Right, X1, X2 };

	struct MouseState { int x = 0, y = 0; };

	class InputManager
	{
	public:
		/* life-cycle -----------------------------------------------------------*/
		static void Initialize();   /* clear everything                           */
		static void Update();       /* call once per *frame* (clears edge sets)   */

		/* binding helpers ------------------------------------------------------*/
		static void BindAction(const std::string& name, SDL_Keycode key);
		static void BindMouseAction(const std::string& name, MouseButton btn);

		static void SaveBindingsToFile(const std::string& file);

		/* event pump -----------------------------------------------------------*/
		static void HandleEvent(const SDL_Event& e);

		/* queries – keyboard ---------------------------------------------------*/
		static bool IsActionHeld(const std::string& name);
		static bool IsActionPressed(const std::string& name);  /* edge */
		static bool IsActionReleased(const std::string& name);  /* edge */

		/* queries – mouse ------------------------------------------------------*/
		static bool IsMouseActionHeld(const std::string& name);
		static bool IsMouseActionPressed(const std::string& name); /* edge */
		static bool IsMouseActionReleased(const std::string& name); /* edge */

		/* misc -----------------------------------------------------------------*/
		static MouseState& GetMouseState();      /* window-coords, modifiable     */
		static int         GetScrollDelta();     /* +ve up / –ve down             */

		/* legacy global (some systems still poke it directly) -----------------*/
		static int gScrollDelta;

	private:
		/* helpers */
		static MouseButton toMouseButton(uint8_t sdlButton);

		/* binding tables */
		static std::unordered_map<std::string, SDL_Keycode> s_keyBind;
		static std::unordered_map<std::string, MouseButton> s_mouseBind;

		/* state – keyboard */
		static std::unordered_set<SDL_Keycode> s_keysDown;
		static std::unordered_set<SDL_Keycode> s_keysPressed;
		static std::unordered_set<SDL_Keycode> s_keysReleased;

		/* state – mouse */
		static std::unordered_set<MouseButton> s_mouseDown;
		static std::unordered_set<MouseButton> s_mousePressed;
		static std::unordered_set<MouseButton> s_mouseReleased;

		/* misc */
		static MouseState s_mouse;
	};

} // namespace WanderSpire
