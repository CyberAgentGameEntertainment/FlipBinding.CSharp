// SPDX-FileCopyrightText: 2025 CyberAgent, Inc.
// SPDX-License-Identifier: MIT

// C API wrapper implementation for FLIP library

#include "flip_capi.h"
#include "../flip/src/cpp/FLIP.h"
#include <cmath>
#include <limits>

extern "C" {

FLIP_API float flip_calculate_ppd(
    float viewing_distance,
    float resolution_x,
    float monitor_width)
{
    return FLIP::calculatePPD(viewing_distance, resolution_x, monitor_width);
}

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
    float** error_map)
{
    // Set up FLIP parameters
    FLIP::Parameters params;

    if (parameters) {
        params.PPD = parameters->ppd;
        params.startExposure = parameters->start_exposure;
        params.stopExposure = parameters->stop_exposure;
        params.numExposures = parameters->num_exposures;
        if (parameters->tonemapper) {
            params.tonemapper = parameters->tonemapper;
        }
    }

    // Call FLIP's pointer-based evaluate function
    float meanFLIPError = 0.0f;

    FLIP::evaluate(
        reference,
        test,
        width,
        height,
        use_hdr != 0,
        params,
        apply_magma_map != 0,
        compute_mean_error != 0,
        meanFLIPError,
        error_map
    );

    // Store mean error if requested
    if (compute_mean_error && mean_error) {
        *mean_error = meanFLIPError;
    }
}

FLIP_API void flip_free(void* ptr)
{
    if (ptr) {
        delete[] static_cast<float*>(ptr);
    }
}

} // extern "C"
