# 🔋 Battery Monitor Widget (Windows Forms - C#)

A lightweight, draggable desktop widget built using Windows Forms in C#. This widget displays real-time battery information such as percentage, charging status, time remaining, and low battery alerts. It also includes auto-start functionality and user settings persistence.

## ✨ Features

- ✅ **Battery Percentage Display**
- ⚡ **Charging Status Indicator**
- ⏳ **Time Remaining Estimation**
- 📉 **Low Battery Notification (below 20%)**
- 📌 **Always-on-top Widget (but can float behind active windows)**
- 🖱️ **Draggable from any point of the window**
- ❌ **Close Button with Confirmation Dialog**
- 🔁 **Auto-start on Windows Boot (via Startup folder shortcut)**
- 💾 **Position Persistence (remembers last screen location)**

## 📷 UI Preview

<img width="228" height="108" alt="image" src="https://github.com/user-attachments/assets/e3b3d2b2-1f03-49f1-bb71-ba582aba8dff" />


## 🛠️ Getting Started

### Prerequisites

- Windows OS
- .NET Framework 4.7.2 or later
- Visual Studio (recommended)

### How to Build

1. Clone or download the repository.
2. Open the solution in Visual Studio.
3. Make sure to add the **COM reference**:
   - `Windows Script Host Object Model` (Required for adding shortcut to Startup)
4. Build and run the project.

### Startup Shortcut

The widget automatically adds a shortcut to your Windows Startup folder unless the user previously chose to **"Close Permanently"**.

## ⚙️ Settings

- `Properties.Settings.Default.ClosePermanenly`: Remembers if the user chose to permanently close the widget.
- `Properties.Settings.Default.WidgetX` / `WidgetY`: Saves the widget's last known screen position.

## 🔔 Notifications

If the battery level falls below **20%** and the system is **not charging**, a balloon tip notification is shown to alert the user.

## 🧩 Libraries & References

- `System.Windows.Forms`
- `System.Drawing`
- `IWshRuntimeLibrary` (COM reference for Startup shortcut)

## 🪟 Behavior

- Double-click maximize is disabled.
- The widget can be repositioned by dragging anywhere on it.
- It updates battery status every **5 seconds**.

## 📁 File Structure

Battery_Monitor/
├── Form1.cs # Main logic for widget UI and functionality
├── Program.cs # App entry point
├── Properties/
│ └── Settings.settings # User setting persistence
└── README.md # This file


## ✅ Future Improvements

- Add dark/light theme toggle
- Option to set custom low battery threshold

## 📃 License

This project is licensed under the MIT License.

---

> Built with ❤️ in C# for those who want simple, distraction-free battery monitoring.
