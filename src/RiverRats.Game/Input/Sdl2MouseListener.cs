using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace RiverRats.Game.Input;

/// <summary>
/// Listens for SDL2 mouse button events via an event filter to reliably detect
/// fast clicks on macOS.
/// <para>
/// <c>Mouse.GetState()</c> polls the instantaneous button state once per frame,
/// which misses press+release cycles that complete between two polls (~16 ms at 60 FPS).
/// This listener installs an SDL2 event filter (<c>SDL_SetEventFilter</c>) that is
/// invoked by SDL as soon as each event arrives — before the game loop processes it.
/// This guarantees every mouse-down and mouse-up is captured regardless of timing.
/// </para>
/// </summary>
internal sealed class Sdl2MouseListener : IDisposable
{
    // SDL2 event constants
    private const uint SDL_MOUSEBUTTONDOWN = 0x401;
    private const uint SDL_MOUSEBUTTONUP = 0x402;
    private const byte SDL_BUTTON_LEFT = 1;

    // Native library name — MonoGame DesktopGL bundles it as libSDL2-2.0.0
    private const string SDL2_LIB = "libSDL2-2.0.0";

    private bool _leftClickBuffered;
    private bool _leftReleaseBuffered;
    private Point _lastEventPosition;
    private bool _installed;

    // Must be stored as a field to prevent the GC from collecting the delegate
    // while it's registered with SDL2.
    private readonly SDL_EventFilter _filterDelegate;
    private readonly IntPtr _filterFunctionPointer;

    /// <summary>
    /// True if a left-button-down event was seen since the last <see cref="ConsumeFrame"/> call.
    /// </summary>
    public bool WasLeftClickedThisFrame => _leftClickBuffered;

    /// <summary>
    /// True if a left-button-up event was seen since the last <see cref="ConsumeFrame"/> call.
    /// </summary>
    public bool WasLeftReleasedThisFrame => _leftReleaseBuffered;

    /// <summary>
    /// Position captured at the time of the last mouse button event.
    /// </summary>
    public Point LastEventPosition => _lastEventPosition;

    public Sdl2MouseListener()
    {
        _filterDelegate = OnSdlEvent;
        _filterFunctionPointer = Marshal.GetFunctionPointerForDelegate(_filterDelegate);
    }

    /// <summary>
    /// Installs the SDL2 event filter. Call once after the game window is created.
    /// Safe to call multiple times — subsequent calls are no-ops.
    /// </summary>
    public void Install()
    {
        if (_installed)
        {
            return;
        }

        try
        {
            SDL_AddEventWatch(_filterFunctionPointer, IntPtr.Zero);
            _installed = true;
        }
        catch (DllNotFoundException)
        {
            // Not running on DesktopGL — silently degrade.
        }
        catch (EntryPointNotFoundException)
        {
            // SDL2 version mismatch — silently degrade.
        }
    }

    /// <summary>
    /// Resets the buffered flags. Call at the end of each frame's input processing.
    /// </summary>
    public void ConsumeFrame()
    {
        _leftClickBuffered = false;
        _leftReleaseBuffered = false;
    }

    public void Dispose()
    {
        if (_installed)
        {
            try
            {
                SDL_DelEventWatch(_filterFunctionPointer, IntPtr.Zero);
            }
            catch
            {
                // Best-effort cleanup.
            }

            _installed = false;
        }
    }

    /// <summary>
    /// SDL event filter callback. Invoked by SDL2 on the main thread as soon as
    /// each event is posted — before <c>SDL_PollEvent</c> returns it.
    /// Returns 1 to keep the event in the queue for MonoGame's normal processing.
    /// </summary>
    private int OnSdlEvent(IntPtr userdata, IntPtr sdlEventPtr)
    {
        var eventType = (uint)Marshal.ReadInt32(sdlEventPtr, 0);

        if (eventType == SDL_MOUSEBUTTONDOWN || eventType == SDL_MOUSEBUTTONUP)
        {
            // Read the button field — offset depends on SDL_MouseButtonEvent layout.
            // Layout: type(4) + timestamp(4) + windowID(4) + which(4) + button(1)
            var button = Marshal.ReadByte(sdlEventPtr, 16);

            if (button == SDL_BUTTON_LEFT)
            {
                // Read x and y: after button(1) + state(1) + clicks(1) + padding(1) = +4 bytes
                // x is at offset 20, y is at offset 24
                var x = Marshal.ReadInt32(sdlEventPtr, 20);
                var y = Marshal.ReadInt32(sdlEventPtr, 24);

                if (eventType == SDL_MOUSEBUTTONDOWN)
                {
                    _leftClickBuffered = true;
                }
                else
                {
                    _leftReleaseBuffered = true;
                }

                _lastEventPosition = new Point(x, y);
            }
        }

        return 1; // Keep the event in the queue for MonoGame.
    }

    // --- SDL2 P/Invoke ---

    // Callback signature: int (*SDL_EventFilter)(void* userdata, SDL_Event* event)
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SDL_EventFilter(IntPtr userdata, IntPtr sdlEvent);

    [DllImport(SDL2_LIB, CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_AddEventWatch(IntPtr filter, IntPtr userdata);

    [DllImport(SDL2_LIB, CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_DelEventWatch(IntPtr filter, IntPtr userdata);
}
