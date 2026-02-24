import 'dart:io';

// 1. El diccionario (Mapeo de la clase raíz al nombre del archivo final)
// Aquí conectamos el nombre de la clase interna con el nombre del JSON de Arknights.
final Map<String, String> tableMapping = {
  // === Tablas Core / Principales ===
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

  // === Sistemas Específicos ===
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

  // === Modos de Juego (Roguelike, Crisis, Sandbox, etc) ===
  'Torappu_ClimbTowerTable': 'climb_tower_table',
  'Torappu_Battle_Cooperate_CooperateModeBattleData': 'cooperate_battle_table',
  'Torappu_CrisisClientData': 'crisis_table',
  'Torappu_CrisisV2SharedData': 'crisis_v2_table',
  'Torappu_RetroStageTable': 'retro_table',
  'Torappu_RoguelikeTopicTable': 'roguelike_topic_table',
  'Torappu_SandboxPermTable': 'sandbox_perm_table',
  'Torappu_SandboxV2Data': 'sandbox_table',

  // === Archivos Dinámicos / Textos ===
  'Torappu_LevelData': 'prts___levels',
  'Torappu_LanguageData': 'init_text',
  'Torappu_MainText': 'main_text',
};

void main() {
  final inputDir = Directory('rawfbs'); // La carpeta donde DNFBDmp escupió todo
  final outputDir = Directory('fbs'); // La carpeta final organizada

  if (!outputDir.existsSync()) {
    outputDir.createSync(recursive: true);
  }

  tableMapping.forEach((rootClass, finalName) {
    File rootFile = File('${inputDir.path}/$rootClass.fbs');

    if (rootFile.existsSync()) {
      print('📦 Empaquetando: $finalName.fbs (desde $rootClass)');

      Set<String> processedFiles = {}; // Para no copiar la misma clase dos veces
      List<String> bundledLines = [];

      // Función recursiva para leer un archivo y sus dependencias
      void processFile(File file) {
        if (processedFiles.contains(file.path)) return;
        processedFiles.add(file.path);

        List<String> lines = file.readAsLinesSync();
        for (String line in lines) {
          if (line.startsWith('include')) {
            // Extraer el nombre del archivo incluido: include "Algo.fbs";
            final match = RegExp(r'include\s+"([^"]+)";').firstMatch(line);
            if (match != null) {
              String includedFileName = match.group(1)!;
              File includedFile = File('${inputDir.path}/$includedFileName');
              if (includedFile.existsSync()) {
                processFile(includedFile); // Llamada recursiva
              }
            }
          } else {
            // Si no es un include, añadimos la línea al archivo final
            bundledLines.add(line);
          }
        }
      }

      processFile(rootFile);

      // Guardar el archivo gigante final
      File finalFile = File('${outputDir.path}/$finalName.fbs');
      finalFile.writeAsStringSync(bundledLines.join('\n'));
    } else {
      print('⚠️ No se encontró la clase raíz: $rootClass.fbs');
    }
  });

  print('✅ ¡Empaquetado completado!');
}
