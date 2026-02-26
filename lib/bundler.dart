import 'dart:io';
import 'package:path/path.dart' as p;

final Map<String, String> tableMapping = {
  'Torappu_CharacterData': 'character_table',
  'Torappu_BuildingData': 'building_data',
  'Torappu_BuildingData_BuildingLocalData': 'building_local_data',
  'Torappu_ActivityTable': 'activity_table',
  'Torappu_StageTable': 'stage_table',
  'Torappu_SkillDataBundle': 'skill_table',
  'Torappu_SkinTable': 'skin_table',
  'Torappu_GachaData': 'gacha_table',
  'Torappu_ItemData': 'item_table',
  'Torappu_EnemyDatabase': 'enemy_database',
  'Torappu_EnemyHandBookDataGroup': 'enemy_handbook_table',
  'Torappu_GameDataConsts': 'gamedata_const',
  'Torappu_ZoneTable': 'zone_table',
  'Torappu_ChapterData': 'chapter_table',
  'Torappu_Audio_Middleware_Data_TorappuAudioData': 'audio_data',
  'Torappu_BattleEquipPack': 'battle_equip_table',
  'Torappu_BuffData': 'buff_table',
  'Torappu_CampaignTable': 'campaign_table',
  'Torappu_CharmData': 'charm_table',
  'Torappu_CharWordTable': 'charword_table',
  'Torappu_CharMasterBasicData': 'char_master_table',
  'Torappu_CharMetaTable': 'char_meta_table',
  'Torappu_CharPatchData': 'char_patch_table',
  'Torappu_CheckInTable': 'checkin_table',
  'Torappu_MeetingClueData': 'clue_data',
  'Torappu_DisplayMetaData': 'display_meta_table',
  'Torappu_EPBreakBuffData': 'ep_breakbuff_table',
  'Torappu_ExtraBattleLogData': 'extra_battlelog_table',
  'Torappu_FavorTable': 'favor_table',
  'Torappu_HandbookInfoTable': 'handbook_info_table',
  'Torappu_HandbookTeamData': 'handbook_team_table',
  'Torappu_HotUpdateMetaTable': 'hotupdate_meta_table',
  'Torappu_Battle_Legion_LegionModeBuffData': 'legion_mode_buff_table',
  'Torappu_Battle_LevelScriptDataMap': 'level_script_table',
  'Torappu_MedalGroupData': 'medal_table',
  'Torappu_MetaUIDisplayTable': 'meta_ui_table',
  'Torappu_MissionTable': 'mission_table',
  'Torappu_OpenServerData': 'open_server_table',
  'Torappu_ReplicateTable': 'replicate_table',
  'Torappu_Resource_ResourceManifest': 'resource_manifest',
  'Torappu_ShopClientData': 'shop_client_table',
  'Torappu_SpecialOperatorTable': 'special_operator_table',
  'Torappu_StoryReviewMetaTable': 'story_review_meta_table',
  'Torappu_StoryReviewGroupClientData': 'story_review_table',
  'Torappu_StoryData': 'story_table',
  'Torappu_TipTable': 'tip_table',
  'Torappu_TokenData': 'token_table',
  'Torappu_UniEquipTable': 'uniequip_table',
  'Torappu_ClimbTowerTable': 'climb_tower_table',
  'Torappu_Battle_Cooperate_CooperateModeBattleData': 'cooperate_battle_table',
  'Torappu_CrisisClientData': 'crisis_table',
  'Torappu_CrisisV2SharedData': 'crisis_v2_table',
  'Torappu_RetroStageTable': 'retro_table',
  'Torappu_RoguelikeTopicTable': 'roguelike_topic_table',
  'Torappu_SandboxPermTable': 'sandbox_perm_table',
  'Torappu_SandboxV2Data': 'sandbox_table',
  'Torappu_LevelData': 'prts___levels',
  'Torappu_LanguageData': 'init_text',
  'Torappu_MainText': 'main_text',
};

/// Bundles modular raw FBS files into single monolithic schemas.
void bundleFbs(String inputDirPath, String outputDirPath) {
  final inputDir = Directory(inputDirPath);
  final outputDir = Directory(outputDirPath);

  if (!outputDir.existsSync()) {
    outputDir.createSync(recursive: true);
  }

  tableMapping.forEach((rootClass, finalName) {
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
            continue;
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
      bundledLines.add('root_type $rootClass;');

      File finalFile = File(p.join(outputDir.path, '$finalName.fbs'));
      finalFile.writeAsStringSync(bundledLines.join('\n'));
    } else {
      print('Missing root class for bundling: $rootClass.fbs');
    }
  });

  print('Bundling completed for: $outputDirPath');
}
