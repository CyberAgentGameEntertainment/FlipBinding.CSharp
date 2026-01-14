// SPDX-FileCopyrightText: 2026 CyberAgent, Inc.
// SPDX-License-Identifier: MIT

namespace FlipBinding.CSharp
{
    /// <summary>
    /// Tonemapper options for HDR-FLIP processing.
    /// </summary>
    /// <remarks>
    /// Why not <c>ToneMapper</c>?
    /// FLIP Native uses <c>Tonemapper</c>, so we used that.
    /// </remarks>
    public enum Tonemapper
    {
        /// <summary>
        /// ACES filmic tonemapping (default).
        /// </summary>
        Aces = 0,

        /// <summary>
        /// Reinhard tonemapping.
        /// </summary>
        Reinhard = 1,

        /// <summary>
        /// Hable/Uncharted 2 tonemapping.
        /// </summary>
        Hable = 2
    }
}
