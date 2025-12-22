// SPDX-FileCopyrightText: 2025 CyberAgent, Inc.
// SPDX-License-Identifier: MIT

using System;

namespace FlipBinding.CSharp
{
    /// <summary>
    /// Result of a FLIP evaluation.
    /// </summary>
    public readonly struct FlipResult
    {
        /// <summary>
        /// Mean FLIP error value in the range [0, 1].
        /// Lower values indicate more similar images.
        /// </summary>
        public float MeanError { get; }

        /// <summary>
        /// Per-pixel error map with values in the range [0, 1].
        /// The array has width * height elements in row-major order.
        /// </summary>
        public float[] ErrorMap { get; }

        /// <summary>
        /// Has valid error map data.
        /// </summary>
        public bool HasErrorMap => ErrorMap != null && ErrorMap.Length > 0;

        /// <summary>
        /// Image width in pixels.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Image height in pixels.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Whether the error map uses Magma colormap (RGB format).
        /// If true, ErrorMap has width * height * 3 elements (interleaved RGB).
        /// If false, ErrorMap has width * height elements (grayscale).
        /// </summary>
        public bool IsMagmaMap { get; }

        /// <summary>
        /// Creates a new FLIP result.
        /// </summary>
        /// <param name="meanError">Mean error value.</param>
        /// <param name="errorMap">Per-pixel error map.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="isMagmaMap">Whether the error map uses Magma colormap.</param>
        public FlipResult(float meanError, float[] errorMap, int width, int height, bool isMagmaMap)
        {
            MeanError = meanError;
            ErrorMap = errorMap;
            Width = width;
            Height = height;
            IsMagmaMap = isMagmaMap;
        }

        /// <summary>
        /// Gets the error value at a specific pixel location.
        /// Only available when IsMagmaMap is false (grayscale mode).
        /// </summary>
        /// <param name="x">X coordinate (0 to Width-1).</param>
        /// <param name="y">Y coordinate (0 to Height-1).</param>
        /// <returns>Error value at the specified location.</returns>
        /// <exception cref="InvalidOperationException">Thrown when IsMagmaMap is true.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when coordinates are out of bounds.</exception>
        public float GetPixel(int x, int y)
        {
            if (!HasErrorMap)
                throw new InvalidOperationException("Error map data is not available.");
            if (IsMagmaMap)
                throw new InvalidOperationException(
                    "GetPixel is not available for Magma map. Use GetPixelRgb instead.");
            if (x < 0 || x >= Width)
                throw new ArgumentOutOfRangeException(nameof(x), x, $"X must be in range [0, {Width - 1}]");
            if (y < 0 || y >= Height)
                throw new ArgumentOutOfRangeException(nameof(y), y, $"Y must be in range [0, {Height - 1}]");

            return ErrorMap[y * Width + x];
        }

        /// <summary>
        /// Gets the RGB color values at a specific pixel location.
        /// Only available when IsMagmaMap is true (Magma colormap mode).
        /// </summary>
        /// <param name="x">X coordinate (0 to Width-1).</param>
        /// <param name="y">Y coordinate (0 to Height-1).</param>
        /// <returns>Tuple of (R, G, B) values in the range [0, 1].</returns>
        /// <exception cref="InvalidOperationException">Thrown when IsMagmaMap is false.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when coordinates are out of bounds.</exception>
        public (float R, float G, float B) GetPixelRgb(int x, int y)
        {
            if (!HasErrorMap)
                throw new InvalidOperationException("Error map data is not available.");
            if (!IsMagmaMap)
                throw new InvalidOperationException(
                    "GetPixelRgb is only available for Magma map. Use GetPixel instead.");
            if (x < 0 || x >= Width)
                throw new ArgumentOutOfRangeException(nameof(x), x, $"X must be in range [0, {Width - 1}]");
            if (y < 0 || y >= Height)
                throw new ArgumentOutOfRangeException(nameof(y), y, $"Y must be in range [0, {Height - 1}]");

            var index = (y * Width + x) * 3;
            return (ErrorMap[index], ErrorMap[index + 1], ErrorMap[index + 2]);
        }
    }
}
