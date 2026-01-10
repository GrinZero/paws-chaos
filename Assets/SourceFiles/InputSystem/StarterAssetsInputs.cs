using UnityEngine;
using System;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;

		[Header("Skill Input Values")]
		// 技能输入状态字段
		public bool skill1;
		public bool skill2;
		public bool skill3;
		public bool capture;
		public bool struggle;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

		// 技能按下事件
		public event Action OnSkill1Pressed;
		public event Action OnSkill2Pressed;
		public event Action OnSkill3Pressed;
		public event Action OnCapturePressed;
		public event Action OnStrugglePressed;
		public event Action OnJumpPressed;



#if ENABLE_INPUT_SYSTEM
		

		
		public void OnMove(InputValue value)
		{
			var moveValue = value.Get<Vector2>();
			Debug.Log($"[StarterAssetsInputs] OnMove callback: {moveValue}");
			MoveInput(moveValue);
		}
		
		// 兼容 PlayerInput 组件的事件绑定（InputMove 是旧的命名）
		public void InputMove(InputAction.CallbackContext context)
		{
			MoveInput(context.ReadValue<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}
		
		// 兼容 PlayerInput 组件的事件绑定
		public void InputLook(InputAction.CallbackContext context)
		{
			if(cursorInputForLook)
			{
				LookInput(context.ReadValue<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}

		// 技能1回调 (键盘1键 / 手柄buttonWest)
		public void OnSkill1(InputValue value)
		{
			Skill1Input(value.isPressed);
			if (value.isPressed)
			{
				OnSkill1Pressed?.Invoke();
			}
		}

		// 技能2回调 (键盘2键 / 手柄buttonNorth)
		public void OnSkill2(InputValue value)
		{
			Skill2Input(value.isPressed);
			if (value.isPressed)
			{
				OnSkill2Pressed?.Invoke();
			}
		}

		// 技能3回调 (键盘3键 / 手柄buttonEast)
		public void OnSkill3(InputValue value)
		{
			Skill3Input(value.isPressed);
			if (value.isPressed)
			{
				OnSkill3Pressed?.Invoke();
			}
		}

		// 捕获回调 (键盘E键 / 手柄buttonSouth)
		public void OnCapture(InputValue value)
		{
			CaptureInput(value.isPressed);
			if (value.isPressed)
			{
				OnCapturePressed?.Invoke();
			}
		}

		// 挣扎回调 (键盘Space键 / 手柄rightTrigger)
		public void OnStruggle(InputValue value)
		{
			StruggleInput(value.isPressed);
			if (value.isPressed)
			{
				OnStrugglePressed?.Invoke();
			}
		}
#endif

		private void Awake()
		{
			SetCursorState(cursorLocked);
			Cursor.visible = false;
		}
		
		/// <summary>
		/// 直接从 Gamepad 读取输入，绕过 PlayerInput 的消息传递问题。
		/// 遍历所有 Gamepad 设备，因为 OnScreenControl 可能创建多个虚拟设备。
		/// </summary>
		private void Update()
		{
			#if ENABLE_INPUT_SYSTEM
			var gamepads = Gamepad.all;
			if (gamepads.Count == 0) 
			{
				move = Vector2.zero;
				return;
			}
			
			// 遍历所有 Gamepad，合并输入（OnScreenStick 和 OnScreenButton 可能在不同设备上）
			Vector2 totalMove = Vector2.zero;
			bool anySkill1 = false, anySkill2 = false, anySkill3 = false, anyCapture = false;
			bool skill1Pressed = false, skill2Pressed = false, skill3Pressed = false, capturePressed = false;
			
			foreach (var gamepad in gamepads)
			{
				// 合并摇杆输入（取最大值）
				Vector2 stickValue = gamepad.leftStick.ReadValue();
				if (stickValue.sqrMagnitude > totalMove.sqrMagnitude)
				{
					totalMove = stickValue;
				}
				
				// 检查按钮状态
				if (gamepad.buttonWest.isPressed) anySkill1 = true;
				if (gamepad.buttonNorth.isPressed) anySkill2 = true;
				if (gamepad.buttonEast.isPressed) anySkill3 = true;
				if (gamepad.buttonSouth.isPressed) anyCapture = true;
				
				if (gamepad.buttonWest.wasPressedThisFrame) skill1Pressed = true;
				if (gamepad.buttonNorth.wasPressedThisFrame) skill2Pressed = true;
				if (gamepad.buttonEast.wasPressedThisFrame) skill3Pressed = true;
				if (gamepad.buttonSouth.wasPressedThisFrame) capturePressed = true;
				
				// 检查跳跃（rightShoulder 或 leftShoulder）
				if (gamepad.rightShoulder.wasPressedThisFrame || gamepad.leftShoulder.wasPressedThisFrame)
				{
					jump = true;
					OnJumpPressed?.Invoke();
				}
			}
			
			// 应用移动输入
			move = totalMove;
			
			// 应用技能输入
			skill1 = anySkill1;
			skill2 = anySkill2;
			skill3 = anySkill3;
			capture = anyCapture;
			
			// 触发事件（添加调试日志）
			if (skill1Pressed)
			{
				Debug.Log("[StarterAssetsInputs] Skill1 (buttonWest) 按下");
				OnSkill1Pressed?.Invoke();
			}
			if (skill2Pressed)
			{
				Debug.Log("[StarterAssetsInputs] Skill2 (buttonNorth) 按下");
				OnSkill2Pressed?.Invoke();
			}
			if (skill3Pressed)
			{
				Debug.Log("[StarterAssetsInputs] Skill3 (buttonEast) 按下");
				OnSkill3Pressed?.Invoke();
			}
			if (capturePressed)
			{
				Debug.Log("[StarterAssetsInputs] Capture (buttonSouth) 按下");
				OnCapturePressed?.Invoke();
			}
			#endif
		}

		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
			// 调试日志：追踪移动输入来源
			if (newMoveDirection.sqrMagnitude > 0.01f)
			{
				Debug.Log($"[StarterAssetsInputs] MoveInput received: {newMoveDirection}");
			}
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

		// 技能输入方法
		public void Skill1Input(bool newSkill1State)
		{
			skill1 = newSkill1State;
		}

		public void Skill2Input(bool newSkill2State)
		{
			skill2 = newSkill2State;
		}

		public void Skill3Input(bool newSkill3State)
		{
			skill3 = newSkill3State;
		}

		public void CaptureInput(bool newCaptureState)
		{
			capture = newCaptureState;
		}

		public void StruggleInput(bool newStruggleState)
		{
			struggle = newStruggleState;
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
			Cursor.visible = !newState;  
			

		}
	}
	
}