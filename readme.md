This fixes a bug in the GOG version of The Zachtronics Solitaire Collection on Windows.

When the app is minimized, the frame rate is no longer governed properly, and it can increase infinitely until it consumes 100% of a CPU or core.

This patch changes the app to use a minimal frame rate when minimized.  The frame rate is restored when the app is restored or maximized.

You can get `SolitairePatcher.exe` from the releases if you like.

To build it yourself, clone the source and then run `build.bat`.

To patch:

1. Run `SolitairePatcher.exe`.
2. Browse to the location of the Zachtronics Solitaire app.
3. Click Apply patch.  The original app is backed up, and a patched version is created.
4. The app creates a small file to remember the location of the app.

To remove the patch, click Restore.
