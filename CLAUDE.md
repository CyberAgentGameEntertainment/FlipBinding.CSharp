# FLIP C# Binding Library

Make [FLIP](https://github.com/NVlabs/flip) available from C# (including Unity).

## Requirements

- .NET SDK 10.0 or later
- CMake 3.9 or later (for native library build)
- C++17 compiler

## Structure

### Directories
- documentation: Design documents
- flip: FLIP added as a submodule
- FlipBinding.CSharp: Binding library
- flip-native: C wrapper for FLIP library

### Dependencies
- Target framework: netstandard2.0 (for Unity compatibility)

## Getting Started

```bash
git clone --recursive https://github.com/CyberAgentGameEntertainment/FlipBinding.CSharp.git
```

## Build Native Library

### macOS (Universal Binary)

```bash
cd flip-native

# Build for arm64
mkdir build-arm64 && cd build-arm64
cmake .. -DCMAKE_OSX_ARCHITECTURES=arm64
cmake --build . --config Release
cd ..

# Build for x86_64
mkdir build-x64 && cd build-x64
cmake .. -DCMAKE_OSX_ARCHITECTURES=x86_64
cmake --build . --config Release
cd ..

# Create Universal Binary
lipo -create \
  build-arm64/libflip_native.dylib \
  build-x64/libflip_native.dylib \
  -output libflip_native.dylib

# Verify
lipo -info libflip_native.dylib
```

### Windows / Linux

```bash
cd flip-native
mkdir build && cd build
cmake ..
cmake --build . --config Release
```

### Move to runtimes folder

- macOS: `libflip_native.dylib` → `FlipBinding.CSharp/runtimes/osx/native/`
- Windows: `flip_native.dll` → `FlipBinding.CSharp/runtimes/win-x64/native/`
- Linux: `libflip_native.so` → `FlipBinding.CSharp/runtimes/linux-x64/native/`

## Build

```bash
dotnet build --configuration Release
```

## Pack

```bash
dotnet pack FlipBinding.CSharp/FlipBinding.CSharp.csproj -o ./artifacts
```

## Test

```bash
dotnet test
```
