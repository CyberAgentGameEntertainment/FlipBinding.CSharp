# SPDX-FileCopyrightText: 2026 CyberAgent, Inc.
# SPDX-License-Identifier: MIT

cmake_minimum_required(VERSION 3.9)
project(flip_native VERSION 1.0.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# FLIP library settings
option(FLIP_ENABLE_CUDA "Enable CUDA support" OFF)

# OpenMP for multithreading
find_package(OpenMP)

# Build shared library
add_library(flip_native SHARED
    flip_capi.cpp
)

target_include_directories(flip_native PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/../flip/src/cpp
)

# Compiler-specific settings
if(MSVC)
    target_compile_options(flip_native PRIVATE /W4)
    target_compile_definitions(flip_native PRIVATE FLIP_EXPORTS)
else()
    target_compile_options(flip_native PRIVATE -Wall -Wextra -fvisibility=hidden)
    if(OpenMP_CXX_FOUND)
        target_link_libraries(flip_native PRIVATE OpenMP::OpenMP_CXX)
    endif()
endif()

# macOS-specific settings
if(APPLE)
    set_target_properties(flip_native PROPERTIES
        MACOSX_RPATH ON
        INSTALL_RPATH "@executable_path"
    )

    # Support cross-compilation for different architectures
    if(CMAKE_OSX_ARCHITECTURES)
        message(STATUS "Building for architectures: ${CMAKE_OSX_ARCHITECTURES}")
    endif()
endif()

# Install targets
install(TARGETS flip_native
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
    RUNTIME DESTINATION bin
)

install(FILES flip_capi.h
    DESTINATION include
)
