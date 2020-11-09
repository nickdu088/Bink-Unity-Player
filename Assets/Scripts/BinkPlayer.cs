using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class BinkPlayer : MonoBehaviour
{

    internal static class BinkNativeMethods
    {
        //=======================================================================

        public const ulong BINKOLDFRAMEFORMAT = 0x00008000L; // using the old Bink frame format (internal use only)
        public const ulong BINKCUSTOMCOLORSPACE = 0x00010000L; // file contains a custom colorspace matrix (only internal now)

        public const ulong BINKNOYPLANE = 0x00000200L; // Don't decompress the Y plane (internal flag)
        public const ulong BINKWILLLOOP = 0x00000080L; // You are planning to loop this Bink.

        public const ulong BINKYCRCBNEW = 0x00000010L; // File uses the new ycrcb colorspace (usually Bink 2)
        public const ulong BINKHDR = 0x00000004L; // Video is an HDR video
        public const ulong BINK_SLICES_2 = 0x00000000L; // Bink 2 file has two slices
        public const ulong BINK_SLICES_3 = 0x00000001L; // Bink 2 file has three slices
        public const ulong BINK_SLICES_4 = 0x00000002L; // Bink 2 file has four slices
        public const ulong BINK_SLICES_8 = 0x00000003L; // Bink 2 file has eight slices
        public const ulong BINK_SLICES_MASK = 0x00000003L; // mask against openflags to get the slice flags

        [DllImport("bink2w64")]
        internal static extern void BinkClose(IntPtr Bink);

        [DllImport("bink2w64")]
        internal static extern IntPtr BinkOpen(string name, BINK_OPEN_FLAGS flags);

        [DllImport("bink2w64")]
        internal static extern string BinkGetError();

        [DllImport("bink2w64")]
        internal static extern IntPtr BinkDoFrame(IntPtr Bink);

        [DllImport("bink2w64")]
        internal static extern int BinkWait(IntPtr bink);

        [DllImport("bink2w64")]
        internal static extern void BinkNextFrame(IntPtr bink);

        [DllImport("bink2w64")]
        internal static extern int BinkCopyToBuffer(IntPtr bink, byte[] dest_addr, int dest_pitch, uint dest_height, uint dest_x, uint dest_y, BINK_COPY_FLAGS copy_flags);

        public struct BINK
        {
            public int Width;
            public int Height;
            public uint Frames;
            public uint FrameNum;
            public uint FrameRate;
            public uint FrameRateDiv;
            public uint ReadError;
            public BINK_OPEN_FLAGS OpenFlags;
            public BINKRECT FrameRects;
            public uint NumRects;
            public uint FrameChangePercent;
        };

        public struct BINKRECT
        {
            public int Left;
            public int Top;
            public int Width;
            public int Height;
        };

        public enum BINK_OPEN_FLAGS : ulong
        {
            BINKFILEOFFSET = 0x00000020L, // Use file offset specified by BinkSetFileOffset
            BINKFILEHANDLE = 0x00800000L, // Use when passing in a file handle
            BINKFROMMEMORY = 0x04000000L, // Use when passing in a pointer to the file
            BINKNOFRAMEBUFFERS = 0x00000400L, // Don't allocate internal frame buffers - application must call BinkRegisterFrameBuffers
            BINKUSETRIPLEBUFFERING = 0x00000008L, // Use triple buffering in the framebuffers
            BINKSNDTRACK = 0x00004000L, // Set the track number to play
            BINKDONTCLOSETHREADS = 0x00000040L, // Don't close threads on BinkClose (call BinkFreeGlobals to close threads)
            BINKGPU = 0x00000100L, // Open Bink in GPU mode
            BINKNOSKIP = 0x00080000L, // Don't skip frames if falling behind
            BINKPRELOADALL = 0x00002000L, // Preload the entire animation
            BINKALPHA = 0x00100000L, // Decompress alpha plane (if present)
            BINKGRAYSCALE = 0x00020000L, // Force Bink to use grayscale
            BINKFRAMERATE = 0x00001000L, // Override fr (call BinkFrameRate first)
            BINKSIMULATE = 0x00400000L, // Simulate the speed (call BinkSim first)
            BINKIOSIZE = 0x01000000L, // Set an io size (call BinkIOSize first)
            BINKNOFILLIOBUF = 0x00200000L, // Don't Fill the IO buffer (in BinkOpen and BinkCopyTo)
            BINKIOPROCESSOR = 0x02000000L, // Set an io processor (call BinkIO first)
            BINKNOTHREADEDIO = 0x08000000L // Don't use a background thread for IO
        }

        public enum BINK_COPY_FLAGS : ulong
        {
            BINKSURFACE32BGRx = 3,
            BINKSURFACE32RGBx = 4,
            BINKSURFACE32BGRA = 5,
            BINKSURFACE32RGBA = 6,
            BINKSURFACE5551 = 8,
            BINKSURFACE555 = 9,
            BINKSURFACE565 = 10,
            BINKSURFACE32ARGB = 12,
            BINKSURFACEMASK = 15,
            BINKGRAYSCALE = 0x00020000L, // Force Bink to use grayscale
            BINKNOSKIP = 0x00080000L, // Don't skip frames if falling behind
            BINKYAINVERT = 0x00000800L // Reverse Y and A planes when blitting (for debugging)
        }
    }

    string BinkAnimation = "topper.bk2";
    BinkNativeMethods.BINK bk;
    Texture2D texture;
    IntPtr bink;
    byte[] buffer;
    // Start is called before the first frame update
    void Start()
    {
        bink = BinkNativeMethods.BinkOpen(BinkAnimation, 0);

        bk = Marshal.PtrToStructure<BinkNativeMethods.BINK>(bink);

        texture = new Texture2D(bk.Width, bk.Height, TextureFormat.RGBA32, false);
        var renderer = GetComponent<SpriteRenderer>();
        renderer.sprite = Sprite.Create(texture, new Rect(0, 0, bk.Width, bk.Height), Vector2.zero);
        buffer = new byte[bk.Width * 4 * bk.Height];
    }

    private void Update()
    {
        if (bink != IntPtr.Zero && BinkNativeMethods.BinkWait(bink) == 0)
        {
            BinkNativeMethods.BinkDoFrame(bink);
            // do you stuff here
            BinkNativeMethods.BinkCopyToBuffer(bink, buffer, bk.Width * 3, (uint)bk.Height, 0, 0, BinkNativeMethods.BINK_COPY_FLAGS.BINKSURFACE32RGBA);

            texture.LoadRawTextureData(buffer);
            texture.Apply();
            BinkNativeMethods.BinkNextFrame(bink);
        }
    }
}
