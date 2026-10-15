// SPDX-FileCopyrightText: 2026 CyberAgent, Inc.
// SPDX-License-Identifier: MIT

// C API wrapper implementation for FLIP library

#include "flip_capi.h"
#include "../flip/src/cpp/FLIP.h"
#include <cmath>
#include <limits>
#include <algorithm>
#include <cstring>

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
    float* peak_error,
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

    // Mirrors FLIP's pointer-based evaluate() instead of calling it,
    // because peak error must be taken from the grayscale map before the magma colormap is applied.
    FLIP::image<FLIP::color3> referenceImage;
    FLIP::image<FLIP::color3> testImage;
    FLIP::image<float> errorMapImage(width, height, 0.0f);
    referenceImage.setPixels(reference, width, height);
    testImage.setPixels(test, width, height);

    FLIP::evaluate(referenceImage, testImage, use_hdr != 0, params, errorMapImage);

#ifdef FLIP_ENABLE_CUDA
    errorMapImage.synchronizeHost();
#endif

    if (compute_mean_error) {
        // Not float: the accumulated rounding error skews the mean on large images.
        double sum = 0.0;
        float peak = 0.0f;
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                const float value = errorMapImage.get(x, y);
                sum += value;
                peak = std::max(peak, value);
            }
        }
        if (mean_error) {
            *mean_error = static_cast<float>(sum / (double(width) * height));
        }
        if (peak_error) {
            *peak_error = peak;
        }
    }

    if (apply_magma_map) {
        *error_map = new float[size_t(width) * height * 3];
        FLIP::image<FLIP::color3> magmaImage(width, height);
        magmaImage.colorMap(errorMapImage, FLIP::magmaMap);
#ifdef FLIP_ENABLE_CUDA
        magmaImage.synchronizeHost();
#endif
        memcpy(*error_map, magmaImage.getHostData(), size_t(width) * height * sizeof(float) * 3);
    } else {
        *error_map = new float[size_t(width) * height];
        memcpy(*error_map, errorMapImage.getHostData(), size_t(width) * height * sizeof(float));
    }
}

FLIP_API void flip_free(void* ptr)
{
    if (ptr) {
        delete[] static_cast<float*>(ptr);
    }
}

} // extern "C"
