using System;
using System.IO;
using System.Security.Cryptography;

internal static class PatchSolitaire
{
    private const string OriginalFrameHash = "F63AA07108EE49EA5AF74CCE2E72373D785A2FAD3355C335F40A9FACB4CD5120";
    private const string PatchedFrameHash = "B1C290A164039B5DE15F0A144AD438753902FD1CFD0C06046588A59B16A69374";

    // File offsets in the Windows GOG March 2023 executable.
    // The fingerprint covers the frame method's header, IL, and exception table.
    private const int FrameOffset = 0x25FE0;
    private const int FrameSize = 4924;
    private const int BeforeRenderOffset = 0x26ABA;
    private const int AfterPresentOffset = 0x26F5B;
    internal const string GameFileName = "The Zachtronics Solitaire Collection.exe";
    private const string ReadScreenStack = "28380E0006";
    private const string JumpToMinimizedCheck = "389E040000";
    private const string OriginalDelayBlock =
        "027B46080004182E09027B46080004193322" +
        "027B390800047B9803000428A70E000620000200005F16FE032D071F142861100006";

    // Windows skipped the old post-present delay block. Reuse that space to
    // test SDL_WINDOW_MINIMIZED before any normal frame rendering starts.
    private const string MinimizedCheck =
        "2B32" +                             // After present, go straight to cleanup.
        "027B390800047B9803000428A70E0006" +  // SDL_GetWindowFlags(mainWindow).
        "1F405F2D0A" +                       // Branch to the idle path if bit 0x40 is set.
        ReadScreenStack + "3843FBFFFF" +     // Replay the call and resume rendering.
        "1F3228611000062B0A" +               // SDL_Delay(50), then normal frame cleanup.
        "00000000000000000000";              // Pad to the original block length.

    private static int Main(string[] args)
    {
        string action = args.Length > 1 ? args[1].ToLowerInvariant() : "apply";
        if (args.Length > 3 || (action != "apply" && action != "check" && action != "restore"))
        {
            Console.Error.WriteLine("Usage: PatchSolitaire.bat [apply | check | restore] [path to game exe]");
            return 2;
        }

        try
        {
            string path = Path.GetFullPath(args.Length == 3 ? args[2] : args[0]);
            if (action == "check")
            {
                Console.WriteLine(IsPatched(path) ? "Patch is applied." : "Ready to apply the patch.");
            }
            else
            {
                Console.WriteLine(SetPatched(path, action == "apply"));
            }
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error.Message);
            return 1;
        }
    }

    internal static bool IsPatched(string path)
    {
        return IsPatched(File.ReadAllBytes(path));
    }

    internal static string SetPatched(string path, bool apply)
    {
        path = Path.GetFullPath(path);
        using (var patchLock = new FileStream(path + ".patch-lock", FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose))
        {
            return UpdateExecutable(path, apply);
        }
    }

    private static bool IsPatched(byte[] image)
    {
        if (image.Length < FrameOffset + FrameSize || image[0] != 'M' || image[1] != 'Z')
        {
            throw new InvalidDataException("This file does not contain the supported Windows GOG frame loop.");
        }

        string hash = Hash(image, FrameOffset, FrameSize);
        if (hash != OriginalFrameHash && hash != PatchedFrameHash)
        {
            throw new InvalidDataException("Unsupported frame loop. This patcher supports the Windows GOG March 2023 build.");
        }
        return hash == PatchedFrameHash;
    }

    private static string UpdateExecutable(string path, bool apply)
    {
        byte[] image = File.ReadAllBytes(path);
        if (IsPatched(image) == apply)
        {
            return apply ? "The patch is already applied." : "The patch is already absent.";
        }

        string currentHash = Hash(image, 0, image.Length);
        WriteHex(image, BeforeRenderOffset, apply ? JumpToMinimizedCheck : ReadScreenStack);
        WriteHex(image, AfterPresentOffset, apply ? MinimizedCheck : OriginalDelayBlock);
        if (IsPatched(image) != apply)
        {
            throw new InvalidDataException("The replacement executable failed verification.");
        }

        string backup = path + ".original";
        string backupToCreate = apply && !File.Exists(backup) ? backup : null;

        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                output.Write(image, 0, image.Length);
                output.Flush(true);
            }
            byte[] current = File.ReadAllBytes(path);
            if (Hash(current, 0, current.Length) != currentHash)
            {
                throw new IOException("The executable changed during patching. Try again when it is no longer being updated.");
            }
            File.Replace(temporary, path, backupToCreate);
        }
        finally
        {
            File.Delete(temporary);
        }

        if (!apply)
        {
            return "Original minimized rendering restored.";
        }
        return backupToCreate != null
            ? "Patch applied. Backup saved next to the game as .exe.original."
            : "Patch applied. The existing .exe.original backup was kept.";
    }

    private static void WriteHex(byte[] image, int offset, string hex)
    {
        for (int index = 0; index < hex.Length; index += 2)
        {
            image[offset++] = Convert.ToByte(hex.Substring(index, 2), 16);
        }
    }

    private static string Hash(byte[] image, int offset, int length)
    {
        using (var sha = SHA256.Create())
        {
            return BitConverter.ToString(sha.ComputeHash(image, offset, length)).Replace("-", "");
        }
    }
}
