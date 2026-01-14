// SPDX-FileCopyrightText: 2026 CyberAgent, Inc.
// SPDX-License-Identifier: MIT

// C API wrapper for FLIP library
// This header provides a C-compatible interface for the FLIP C++ library,
// enabling P/Invoke calls from C# and other languages.
//
// This API is designed to be faithful to FLIP::evaluate() pointer API.

#ifndef FLIP_CAPI_H
#define FLIP_CAPI_H

#ifdef __cplusplus
extern "C" {
#endif

// Export macros
#ifdef _WIN32
    #ifdef FLIP_EXPORTS
        #define FLIP_API __declspec(dllexport)
    #else
        #define FLIP_API __declspec(dllimport)
    #endif
#else
    #define FLIP_API __attribute__((visibility("default")))
#endif

/// Parameters for FLIP evaluation.
/// C-compatible version of FLIP::Parameters.
typedef struct {
    /// Pixels per degree. Default: ~67 (based on 0.7m viewing distance, 3840px width, 0.7m monitor).
    float ppd;
    /// Start exposure for HDR-FLIP. Use INFINITY for auto-calculation.
    float start_exposure;
    /// Stop exposure for HDR-FLIP. Use INFINITY for auto-calculation.
    float stop_exposure;
    /// Number of exposures for HDR-FLIP. Use -1 for auto-calculation.
    int num_exposures;
    /// Tonemapper name for HDR-FLIP: "aces" (default), "reinhard", or "hable".
    const char* tonemapper;
} FlipParameters;

/// Calculates PPD (pixels per degree) from viewing conditions.
///
/// @param viewing_distance Viewing distance from the monitor in meters.
/// @param resolution_x Horizontal resolution of the monitor in pixels.
/// @param monitor_width Physical width of the monitor in meters.
/// @return Calculated PPD value.
FLIP_API float flip_calculate_ppd(
    float viewing_distance,
    float resolution_x,
    float monitor_width
);

/// Evaluates FLIP between a reference image and a test image.
///
/// This function is faithful to FLIP::evaluate() pointer API.
/// Memory for error_map is allocated by this function and must be freed with flip_free().
///
/// @param reference Reference image data in interleaved RGB format [r,g,b,r,g,b,...].
///                  Values should be in [0,1] range for LDR, can exceed for HDR (linear RGB).
/// @param test Test image data in interleaved RGB format (linear RGB).
/// @param width Image width in pixels.
/// @param height Image height in pixels.
/// @param use_hdr 0 for LDR-FLIP, non-zero for HDR-FLIP.
/// @param parameters Pointer to FlipParameters. If NULL, default values are used.
/// @param apply_magma_map If non-zero, output is RGB with magma colormap applied (3 * width * height floats).
///                        If zero, output is grayscale error values (width * height floats).
/// @param compute_mean_error If non-zero, mean FLIP error is computed and stored in mean_error.
/// @param mean_error Output pointer for mean FLIP error value. Can be NULL if compute_mean_error is 0.
/// @param error_map Output pointer to error map. Memory is allocated by this function.
///                  Caller must free with flip_free(). Output size depends on apply_magma_map.
FLIP_API void flip_evaluate(
    const float* reference,
    const float* test,
    int width,
    int height,
    int use_hdr,
    const FlipParameters* parameters,
    int apply_magma_map,
    int compute_mean_error,
    float* mean_error,
    float** error_map
);

/// Frees memory allocated by the library.
///
/// @param ptr Pointer to the memory to free. Safe to call with NULL.
FLIP_API void flip_free(void* ptr);

#ifdef __cplusplus
}
#endif

#endif // FLIP_CAPI_H
