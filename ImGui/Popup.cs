using ImGuiNET;
using NewMath;
using Progrimage;
using Progrimage.Utils;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgrimageImGui
{
	public struct Popup
	{
		public bool IsOpen { get; private set; }
		private bool _suppressNextOpen = false;
		private bool _shouldShow = false;
		private bool _open = false;
		public Action? CloseAction;
		public Action? OpenAction = null;
		public Action DrawAction;
		public string Name;

		#region Properties
		public bool ShouldShow
		{
			get => _shouldShow;
			set
			{
				if (value == _shouldShow) return;
				if (value) Open();
				else _shouldShow = false;
			}
		}
		#endregion

		#region Constructors
		public Popup(string name, Action drawAction)
		{
			Name = name;
			IsOpen = false;
			CloseAction = null;
			DrawAction = drawAction;
		}

		public Popup(string name, Action closeAction, Action drawAction)
		{
			Name = name;
			IsOpen = false;
			CloseAction = closeAction;
			DrawAction = drawAction;
		}
		#endregion

		#region Public Methods
		public void Open()
		{
			IsOpen = true;
			_shouldShow = true;
			_open = true;
		}

		public void Close()
		{
			_shouldShow = false;
		}

		public void SuppressNextOpen()
		{
			_suppressNextOpen |= IsOpen;
		}

		public void Draw()
		{
			if (!_shouldShow) return;

			if (_suppressNextOpen)
			{
				_suppressNextOpen = false;
				IsOpen = false;
				_shouldShow = false;
			}
			else
			{
				if (_open)
				{
					_open = false;
					OpenAction?.Invoke();
					ImGui.OpenPopup(Name);
				}
				IsOpen = ImGui.BeginPopup(Name, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize);
			}

			if (!IsOpen)
			{
				_shouldShow = false;
				_suppressNextOpen = false;
				CloseAction?.Invoke();
				return;
			}

			DrawAction();
			MainWindow.MouseOverCanvasWindow &= !ImGui.IsWindowHovered();
			ImGui.EndPopup();
		}
		#endregion
	}
}
