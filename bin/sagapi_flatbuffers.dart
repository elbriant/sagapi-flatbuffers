import 'dart:io';
import 'package:args/args.dart';
import 'package:path/path.dart' as p;

import 'package:sagapi_flatbuffers/extract_fbs.dart';
import 'package:sagapi_flatbuffers/bundler.dart';

const String version = '0.0.1';

// Package mappings based on server type
const Map<String, String> serverPackages = {
  'global': 'com.YoStarJP.Arknights',
  'cn': 'com.hypergryph.arknights', //placeholder
  'tw': 'tw.txwy.and.arknights', //placeholder
};

ArgParser buildParser() {
  return ArgParser()
    ..addFlag('help', abbr: 'h', negatable: false, help: 'Print this usage information.')
    ..addFlag('verbose', abbr: 'v', negatable: false, help: 'Show additional command output.')
    ..addFlag('version', negatable: false, help: 'Print the tool version.')
    ..addOption('server', allowed: ['all', 'global', 'cn', 'tw'], defaultsTo: 'global')
    ..addOption('dumps-path', mandatory: true)
    ..addOption('out-path', mandatory: true);
}

void printUsage(ArgParser argParser) {
  print('Usage: dart sagapi_flatbuffers.dart <flags> [arguments]');
  print(argParser.usage);
}

void main(List<String> arguments) async {
  final ArgParser argParser = buildParser();
  try {
    final ArgResults results = argParser.parse(arguments);
    bool verbose = false;

    // Process the parsed arguments.
    if (results.flag('help')) {
      printUsage(argParser);
      return;
    }
    if (results.flag('version')) {
      print('sagapi_flatbuffers version: $version');
      return;
    }
    if (results.flag('verbose')) {
      verbose = true;
    }
    final String targetServer = results['server'];
    final String dumpsRoot = results['dumps-path'];
    final String outputRoot = results['out-path'];

    if (verbose) {
      print('[VERBOSE] All arguments: ${results.arguments}');
    }

    List<String> serversToProcess = targetServer == 'all'
        ? serverPackages.keys.toList()
        : [targetServer];

    for (String server in serversToProcess) {
      print('\n=== Processing Server: ${server.toUpperCase()} ===');
      String packageName = serverPackages[server]!;

      // Path resolution: e.g., ../dumps_repo/DummyDlls/com.YoStarJP.Arknights/DummyDll
      String dummyDllPath = p.join(dumpsRoot, packageName, 'DummyDll');

      if (!Directory(dummyDllPath).existsSync()) {
        print('[WARN] DummyDll path not found for $server: $dummyDllPath. Skipping.');
        continue;
      }

      String rawOutPath = p.join(outputRoot, 'rawfbs', server);
      String bundledOutPath = p.join(outputRoot, server);

      // 1. Run extraction phase
      bool extractSuccess = await extractFbs(dummyDllPath, rawOutPath);

      // 2. Run bundling phase
      if (extractSuccess) {
        bundleFbs(rawOutPath, bundledOutPath);
        print('[OK] Server ${server.toUpperCase()} completed successfully.');
      } else {
        print('[ERROR] Failed to process server $server.');
      }
    }
  } on FormatException catch (e) {
    // Print usage information if an invalid argument was provided.
    print('[ERROR] Argument parsing error: $e');
    print('');
    printUsage(argParser);
    print(
      'Usage: dart run bin/sagapi_flatbuffers.dart --server <server> --dumps-path <path> --out-path <path>',
    );
    exit(1);
  }
}
