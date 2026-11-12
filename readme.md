This fixes a bug in the GOG version of The Zachtronics Solitaire Collection on Windows.

When the app is minimized, the frame rate is no longer governed properly, and it can increase infinitely until it consumes 100% of a CPU or core.

This patch changes the app to use a minimal frame rate when minimized.  The frame rate is restored when the app is restored or maximized.

1. Run `build.bat` to create `SolitairePatcher.exe`.
2. Run `SolitairePatcher.exe`.
3. Browse to the location of the Zachtronics Solitaire app.
4. Click Apply patch.  The original app is backed up, and a patched version is created.
5. The app creates a small file to remember the location of the app.

To remove the patch, click Restore.