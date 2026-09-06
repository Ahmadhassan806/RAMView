# RAM VIEW — Troubleshooting & FAQ

## ❓ Frequently Asked Questions & Solutions

### 1. Why do some system processes show "Access Denied" or no path/icon?
**Cause**: Windows NT security architecture restricts non-elevated user processes from inspecting protected system processes, protected anti-cheat processes, or other user login sessions.
**Expected Behavior**: This is standard, secure Windows behavior (§7, §23). RAM VIEW still queries their physical Working Set memory via Windows kernel counters without requiring Administrator elevation. If run as Administrator, additional module paths will become accessible.

### 2. How do I bring back the window if I minimized or closed it?
- Click or double-click the **RAM VIEW** icon in the Windows System Tray (near the system clock).
- Or press the global hotkey: `Ctrl + Shift + R`.

### 3. What does "Ghost" (Click-Through) mode do?
When **Ghost** is activated:
- The overlay window applies the Win32 `WS_EX_TRANSPARENT` style.
- All mouse clicks and wheel events pass directly through the boxes to whatever game, browser, or window is underneath.
- To re-enable interactivity, click the RAM VIEW icon in the system tray, or press `Ctrl + Shift + R` to hide and re-open.

### 4. Why does RAM VIEW refuse to terminate a process?
If you select a core operating system process (such as `csrss.exe`, `services.exe`, `System`, or PID 4) and click "End Process", RAM VIEW will reject the request. Terminating critical operating system processes causes Windows to crash into a Blue Screen of Death (BSOD) or corrupt the active session. This safety lock cannot be bypassed.

### 5. Why do percentages sum to 100% of tracked processes rather than 100% of physical hardware RAM?
As defined in specification §6:
`totalTrackedMemory` is the sum of Working Set across all enumerated and active processes for that cycle. Computing percentages relative to tracked memory ensures that when you filter down to "Applications" or "Highest", the visual treemap remains mathematically consistent and fills the canvas accurately.

### 6. High DPI / Blurry Text on Multiple Monitors
RAM VIEW is declared with `PerMonitorV2` DPI awareness in `app.manifest`. If moving across displays with different scaling factors (e.g. 100% and 150%), the window automatically rescales using DirectX composition without font blurriness.
