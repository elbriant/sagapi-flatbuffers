import 'dart:io';
import 'package:path/path.dart' as p;

/// Extracts raw .fbs files from DummyDlls using the C# tool.
Future<bool> extractFbs(String dummyDllDir, String outputDir) async {
  final String submoduleDir = 'dnfbdump-sagapi';
  final String exeExtension = Platform.isWindows ? '.exe' : '';
  final String exePath = p.join(submoduleDir, 'bin', 'Release', 'net8.0', 'DNFBDmp$exeExtension');

  print('[INFO] Verifying native generator...');

  // Detect if executable exists; if not, compile it.
  if (!File(exePath).existsSync()) {
    print('[INFO] Executable not found. Compiling C# submodule...');
    var buildResult = await Process.run('dotnet', [
      'build',
      '-c',
      'Release',
    ], workingDirectory: p.absolute(submoduleDir));

    if (buildResult.exitCode != 0) {
      print('[ERROR] Critical error compiling DNFBDmp-sagapi:');
      print(buildResult.stderr);
      return false;
    }
    print('[OK] Submodule compiled successfully.');
  }

  final output = Directory(outputDir);
  if (!output.existsSync()) {
    output.createSync(recursive: true);
  }

  print('[INFO] Extracting DummyDlls to FlatBuffers in $outputDir...');

  if (!Platform.isWindows) {
    await Process.run('chmod', ['+x', exePath]);
  }

  var extractResult = await Process.run(p.absolute(exePath), [
    p.absolute(dummyDllDir),
    p.absolute(outputDir),
  ], workingDirectory: Directory.current.path);

  if (extractResult.exitCode == 0) {
    print('[OK] Extraction successful.');
    return true;
  } else {
    print('[ERROR] Extraction error:');
    print(extractResult.stderr);
    return false;
  }
}
