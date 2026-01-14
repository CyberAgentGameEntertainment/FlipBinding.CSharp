// SPDX-FileCopyrightText: 2026 CyberAgent, Inc.
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.InteropServices;

namespace FlipBinding.CSharp
{
    /// <summary>
    /// Native parameters structure for FLIP evaluation.
    /// Mirrors the C FlipParameters struct.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeFlipParameters
    {
        public float Ppd;
        public float StartExposure;
        public float StopExposure;
        public int NumExposures;
        public IntPtr Tonemapper; // const char*
    }

    /// <summary>
    /// Low-level P/Invoke declarations for the FLIP native library.
    /// </summary>
    internal static class FlipNative
    {
#if UNITY_IOS && !UNITY_EDITOR
        private const string DllName = "__Internal";
#else
        private const string DllName = "flip_native";
#endif
        /// <summary>
        /// Calculates PPD (pixels per degree) from viewing conditions.
        /// </summary>
        /// <param name="viewingDistance">Viewing distance from the monitor in meters.</param>
        /// <param name="resolutionX">Horizontal resolution of the monitor in pixels.</param>
        /// <param name="monitorWidth">Physical width of the monitor in meters.</param>
        /// <returns>Calculated PPD value.</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "flip_calculate_ppd")]
        public static extern float CalculatePpd(
            float viewingDistance,
            float resolutionX,
            float monitorWidth
        );

        /// <summary>
        /// Evaluates FLIP between a reference image and a test image.
        /// Memory for errorMap is allocated by the native library and must be freed with Free().
        /// </summary>
        /// <param name="reference">Reference image data in interleaved RGB format.</param>
        /// <param name="test">Test image data in interleaved RGB format.</param>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="useHdr">0 for LDR-FLIP, non-zero for HDR-FLIP.</param>
        /// <param name="parameters">Pointer to FlipParameters. Can be IntPtr.Zero for defaults.</param>
        /// <param name="applyMagmaMap">If non-zero, output is RGB with magma colormap.</param>
        /// <param name="computeMeanError">If non-zero, mean FLIP error is computed.</param>
        /// <param name="meanError">Output mean FLIP error value.</param>
        /// <param name="errorMap">Output pointer to error map (allocated by native).</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "flip_evaluate")]
        public static extern unsafe void Evaluate(
            float* reference,
            float* test,
            int width,
            int height,
            int useHdr,
            NativeFlipParameters* parameters,
            int applyMagmaMap,
            int computeMeanError,
            float* meanError,
            float** errorMap
        );

        /// <summary>
        /// Frees memory allocated by the native library.
        /// </summary>
        /// <param name="ptr">Pointer to the memory to free.</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "flip_free")]
        public static extern void Free(IntPtr ptr);
    }
}
