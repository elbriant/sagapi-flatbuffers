import 'dart:io';
import 'package:path/path.dart' as p;

final Map<String, String> tableMapping = {
  'character_table': 'Torappu_CharacterData',
  'building_data': 'Torappu_BuildingData',
  'building_local_data': 'Torappu_BuildingData_BuildingLocalData',
  'activity_table': 'Torappu_ActivityTable',
  'stage_table': 'Torappu_StageTable',
  'skill_table': 'Torappu_SkillDataBundle',
  'skin_table': 'Torappu_SkinTable',
  'gacha_table': 'Torappu_GachaData',
  'item_table': 'Torappu_InventoryData',
  'enemy_database': 'Torappu_EnemyDatabase',
  'enemy_handbook_table': 'Torappu_EnemyHandBookDataGroup',
  'gamedata_const': 'Torappu_GameDataConsts',
  'zone_table': 'Torappu_ZoneTable',
  'chapter_table': 'Torappu_ChapterData',
  'audio_data': 'Torappu_Audio_Middleware_Data_TorappuAudioData',
  'battle_equip_table': 'Torappu_BattleEquipPack',
  'buff_table': 'Torappu_BuffData',
  'campaign_table': 'Torappu_CampaignTable',
  'charm_table': 'Torappu_CharmData',
  'charword_table': 'Torappu_CharWordTable',
  'char_master_table': 'Torappu_CharMasterBasicData',
  'char_meta_table': 'Torappu_CharMetaTable',
  'char_patch_table': 'Torappu_CharPatchData',
  'checkin_table': 'Torappu_CheckInTable',
  'clue_data': 'Torappu_MeetingClueData',
  'display_meta_table': 'Torappu_DisplayMetaData',
  'ep_breakbuff_table': 'Torappu_EPBreakBuffData',
  'extra_battlelog_table': 'Torappu_ExtraBattleLogData',
  'favor_table': 'Torappu_FavorTable',
  'handbook_info_table': 'Torappu_HandbookInfoTable',
  'handbook_team_table': 'Torappu_HandbookTeamData',
  'hotupdate_meta_table': 'Torappu_HotUpdateMetaTable',
  'legion_mode_buff_table': 'Torappu_Battle_Legion_LegionModeBuffData',
  'level_script_table': 'Torappu_Battle_LevelScriptDataMap',
  'medal_table': 'Torappu_MedalData',
  'meta_ui_table': 'Torappu_MetaUIDisplayTable',
  'mission_table': 'Torappu_MissionTable',
  'replicate_table': 'Torappu_ReplicateTable',
  'resource_manifest': 'Torappu_Resource_ResourceManifest',
  'shop_client_table': 'Torappu_ShopClientData',
  'special_operator_table': 'Torappu_SpecialOperatorTable',
  'story_review_meta_table': 'Torappu_StoryReviewMetaTable',
  'story_review_table': 'Torappu_StoryReviewGroupClientData',
  'story_table': 'Torappu_StoryData',
  'tip_table': 'Torappu_TipTable',
  'climb_tower_table': 'Torappu_ClimbTowerTable',
  'cooperate_battle_table': 'Torappu_Battle_Cooperate_CooperateModeBattleData',
  'crisis_table': 'Torappu_CrisisClientData',
  'crisis_v2_table': 'Torappu_CrisisV2SharedData',
  'retro_table': 'Torappu_RetroStageTable',
  'roguelike_topic_table': 'Torappu_RoguelikeTopicTable',
  'sandbox_perm_table': 'Torappu_SandboxPermTable',
  'prts___levels': 'Torappu_LevelData',
  'open_server_table': 'Torappu_OpenServerSchedule',
  'uniequip_table': 'Torappu_UniEquipTable',
  'main_text': 'Torappu_LanguageData',
  // 'sandbox_table': 'Torappu_Sandbox' , // apparently not used ?

  // "aliased schemas"
  'token_table': 'Torappu_CharacterData', // 'character_table'
  'handbook_table': 'Torappu_HandbookInfoTable', // 'handbook_info_table'
  'init_text': 'Torappu_LanguageData', // 'main_text'
};

// Dict wrapper tables.
final Set<String> _wrappedTables = {
  'chapter_table',
  'char_master_table',
  'character_table',
  'token_table',
  'handbook_team_table',
  'skill_table',
  'story_review_table',
  'story_table',
  'extra_battlelog_table',
  'battle_equip_table',
  'replicate_table',
  'buff_table',
};

/// Bundles modular raw FBS files into single monolithic schemas.
void bundleFbs(String inputDirPath, String outputDirPath) {
  final inputDir = Directory(inputDirPath);
  final outputDir = Directory(outputDirPath);

  if (!outputDir.existsSync()) {
    outputDir.createSync(recursive: true);
  }

  tableMapping.forEach((finalName, rootClass) {
    File rootFile = File(p.join(inputDir.path, '$rootClass.fbs'));

    if (rootFile.existsSync()) {
      Set<String> processedFiles = {};
      List<String> bundledLines = [];

      void processFile(File file) {
        if (processedFiles.contains(file.path)) return;
        processedFiles.add(file.path);

        List<String> lines = file.readAsLinesSync();
        for (String line in lines) {
          if (line.startsWith('include')) {
            final match = RegExp(r'include\s+"([^"]+)";').firstMatch(line);
            if (match != null) {
              String includedFileName = match.group(1)!;
              File includedFile = File(p.join(inputDir.path, includedFileName));
              if (includedFile.existsSync()) {
                processFile(includedFile);
              }
            }
          } else if (line.startsWith('root_type')) {
            continue; // Ignore internal roots
          } else {
            if (line.contains('k__BackingField')) {
              line = line.replaceAll('<', '').replaceAll('>k__BackingField', '');
            }
            bundledLines.add(line);
          }
        }
      }

      processFile(rootFile);
      bundledLines.add('');

      // SimpleKVTable wrapper
      if (_wrappedTables.contains(finalName)) {
        bundledLines.add('table dict__string__$rootClass {');
        bundledLines.add('  dict_key: string(key);');
        bundledLines.add('  dict_value: $rootClass;');
        bundledLines.add('}');
        bundledLines.add('');
        bundledLines.add('table Torappu_SimpleKVTable_$rootClass {');
        bundledLines.add('  dict_array: [dict__string__$rootClass];');
        bundledLines.add('}');
        bundledLines.add('');
        bundledLines.add('root_type Torappu_SimpleKVTable_$rootClass;');
      } else {
        bundledLines.add('root_type $rootClass;');
      }

      File finalFile = File(p.join(outputDir.path, '$finalName.fbs'));
      finalFile.writeAsStringSync(bundledLines.join('\n'));
    } else {
      print('Missing root class for bundling: $rootClass.fbs');
    }
  });

  print('Bundling completed for: $outputDirPath');
}
