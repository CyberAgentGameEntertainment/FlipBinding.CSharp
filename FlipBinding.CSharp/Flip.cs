// SPDX-FileCopyrightText: 2026 CyberAgent, Inc.
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.InteropServices;

namespace FlipBinding.CSharp
{
    /// <summary>
    /// High-level API for FLIP image comparison.
    /// FLIP is a perceptual image difference metric that accounts for human visual system characteristics.
    /// </summary>
    public static class Flip
    {
        // Default PPD value (67.02) based on: 0.7m viewing distance, 3840px resolution, 0.7m monitor width.
        private const float DefaultPpd = 67.0206451f;

        private static readonly IntPtr s_acesPtr;
        private static readonly IntPtr s_reinhardPtr;
        private static readonly IntPtr s_hablePtr;

        static Flip()
        {
            // Pre-allocate tonemapper strings (these live for the lifetime of the application)
            s_acesPtr = Marshal.StringToHGlobalAnsi("aces");
            s_reinhardPtr = Marshal.StringToHGlobalAnsi("reinhard");
            s_hablePtr = Marshal.StringToHGlobalAnsi("hable");
        }

        /// <summary>
        /// Evaluates FLIP between a reference image and a test image.
        /// </summary>
        /// <param name="reference">Reference image data in interleaved RGB format [r,g,b,r,g,b,...]. Values should be in [0,1] range for LDR, can exceed for HDR (linear RGB).</param>
        /// <param name="test">Test image data in interleaved RGB format (linear RGB).</param>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="useHdr">Whether to use HDR mode. LDR: values in [0,1], HDR: values can exceed [0,1].</param>
        /// <param name="ppd">Pixels per degree. Default is 67 (4K display at 0.7m viewing distance).</param>
        /// <param name="tonemapper">Tonemapper for HDR-FLIP processing. Ignored when useHdr is false.</param>
        /// <param name="startExposure">Start exposure for HDR-FLIP. Use float.PositiveInfinity for auto-calculation.</param>
        /// <param name="stopExposure">Stop exposure for HDR-FLIP. Use float.PositiveInfinity for auto-calculation.</param>
        /// <param name="numExposures">Number of exposures for HDR-FLIP. Use -1 for auto-calculation.</param>
        /// <param name="applyMagmaMap">If true, output error map uses Magma colormap (RGB). If false, output is grayscale.</param>
        /// <returns>FLIP evaluation result containing mean error and per-pixel error map.</returns>
        /// <exception cref="ArgumentNullException">Thrown when reference or test is null.</exception>
        /// <exception cref="ArgumentException">Thrown when image dimensions are invalid or arrays have incorrect size.</exception>
        public static unsafe FlipResult Evaluate(
            float[] reference,
            float[] test,
            int width,
            int height,
            bool useHdr = false,
            float ppd = DefaultPpd,
            Tonemapper tonemapper = Tonemapper.Aces,
            float startExposure = float.PositiveInfinity,
            float stopExposure = float.PositiveInfinity,
            int numExposures = -1,
            bool applyMagmaMap = false)
        {
            ValidateInputs(reference, test, width, height);

            // Convert managed parameters to native struct
            var nativeParams = new NativeFlipParameters
            {
                Ppd = ppd,
                StartExposure = startExposure,
                StopExposure = stopExposure,
                NumExposures = numExposures,
                Tonemapper = GetTonemapperPtr(tonemapper)
            };

            float meanError = 0;
            float* errorMapPtr = null;

            fixed (float* pRef = reference)
            fixed (float* pTest = test)
            {
                FlipNative.Evaluate(
                    pRef,
                    pTest,
                    width,
                    height,
                    useHdr ? 1 : 0,
                    &nativeParams,
                    applyMagmaMap ? 1 : 0,
                    1, // computeMeanError = true
                    &meanError,
                    &errorMapPtr
                );
            }

            // Copy error map from native memory to managed array
            // Magma map outputs RGB (3 channels), grayscale outputs 1 channel
            float[] errorMap;
            if (errorMapPtr != null)
            {
                var errorMapSize = applyMagmaMap ? width * height * 3 : width * height;
                errorMap = new float[errorMapSize];
                Marshal.Copy((IntPtr)errorMapPtr, errorMap, 0, errorMapSize);
                FlipNative.Free((IntPtr)errorMapPtr);
            }
            else
            {
                errorMap = Array.Empty<float>();
            }

            return new FlipResult(meanError, errorMap, width, height, applyMagmaMap);
        }

        /// <summary>
        /// Calculates PPD (pixels per degree) from viewing conditions.
        /// </summary>
        /// <param name="viewingDistance">Viewing distance from the monitor in meters. Default is 0.7m.</param>
        /// <param name="resolutionX">Horizontal resolution of the monitor in pixels. Default is 3840 (4K).</param>
        /// <param name="monitorWidth">Physical width of the monitor in meters. Default is 0.7m.</param>
        /// <returns>Calculated PPD value.</returns>
        /// <remarks>
        /// Formula: PPD = viewingDistance * (resolutionX / monitorWidth) * (PI / 180)
        /// Default values (0.7m, 3840, 0.7m) yield approximately 67 PPD.
        /// </remarks>
        public static float CalculatePpd(
            float viewingDistance = 0.7f,
            int resolutionX = 3840,
            float monitorWidth = 0.7f)
        {
            return FlipNative.CalculatePpd(viewingDistance, resolutionX, monitorWidth);
        }

        private static IntPtr GetTonemapperPtr(Tonemapper tonemapper)
        {
            return tonemapper switch
            {
                Tonemapper.Aces => s_acesPtr,
                Tonemapper.Reinhard => s_reinhardPtr,
                Tonemapper.Hable => s_hablePtr,
                _ => s_acesPtr
            };
        }

        private static void ValidateInputs(float[] reference, float[] test, int width, int height)
        {
            if (reference == null)
                throw new ArgumentNullException(nameof(reference));
            if (test == null)
                throw new ArgumentNullException(nameof(test));
            if (width <= 0)
                throw new ArgumentException("Width must be positive.", nameof(width));
            if (height <= 0)
                throw new ArgumentException("Height must be positive.", nameof(height));

            var expectedSize = width * height * 3; // RGB interleaved
            if (reference.Length != expectedSize)
                throw new ArgumentException(
                    $"Reference array size ({reference.Length}) does not match expected size ({expectedSize}).",
                    nameof(reference));
            if (test.Length != expectedSize)
                throw new ArgumentException(
                    $"Test array size ({test.Length}) does not match expected size ({expectedSize}).", nameof(test));
        }
    }
}
