# Sagapi FlatBuffers

An automated extraction and bundling pipeline for Arknights FlatBuffers schemas (.fbs).

This repository contains the orchestration tools required to extract raw FlatBuffers schemas from Arknights' DummyDlls and bundle them into clean, compiled schemas ready for `flatc` JSON generation. It supports cross-server extraction (Global, CN, TW).

## Architecture

The extraction pipeline consists of two main phases:
1. **Extraction (C# / .NET 8):** Uses a modified version of DNFBDmp to decompile `Torappu` classes from DummyDlls into raw `.fbs` files, fixing internal inheritance and backing field names.
2. **Bundling (Dart):** Resolves `#include` dependencies, cleans up specific syntax bugs, and bundles the modular `.fbs` files into monolithic schemas compatible with the MooncellWiki format.

## Prerequisites

To run this pipeline locally, you need:
* [Dart SDK](https://dart.dev/get-dart) (>=3.11.0)
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* The `sagapi-dumper` repository containing the latest DummyDlls.

## Usage

You can run the orchestrator from the command line using Dart.

```bash
dart run bin/sagapi_flatbuffers.dart --server <target_server> --dumps-path <path_to_DummyDlls> --out-path <output_directory>
```

**Arguments:**

* `--server`: The target server to process. Options: `global`, `cn`, `tw`, or `all`. (Default: `global`)
* `--dumps-path`: The root directory containing the extracted server packages (e.g., `./sagapi-dumper/DummyDlls`).
* `--out-path`: The destination folder where the generated schemas will be saved.
